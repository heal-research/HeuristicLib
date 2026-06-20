using System.Collections.Concurrent;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Problems.Dynamic;

// ToDo: A DynamicProblem should be, foremost, a Problem. It "being" also an Observer, is an interesting way of implementing about it, but we have to think if this is really what we want.
public abstract class DynamicProblem<TGenotype, TSearchSpace> :
    SingleSolutionProblem<TGenotype, TSearchSpace>,
    IDynamicProblem<TGenotype, TSearchSpace>,
    IEvaluatorObserver<TGenotype, TSearchSpace, DynamicProblem<TGenotype, TSearchSpace>>,
    IInterceptorObserver<TGenotype, TSearchSpace, DynamicProblem<TGenotype, TSearchSpace>, ISearchState>,
    IDisposable
    where TSearchSpace : class, ISearchSpace<TGenotype>
{
    private readonly ConcurrentBag<(TGenotype genotype, ObjectiveVector objective, EvaluationTiming timing)> evaluationLog = [];
    private readonly ReaderWriterLockSlim rwLock = new();
    private readonly UpdatePolicy updatePolicy;
    private bool disposed;

    protected DynamicProblem(Objective objective, TSearchSpace searchSpace, IRandomNumberGenerator environmentRandom, UpdatePolicy updatePolicy = UpdatePolicy.AfterEvaluation, int epochLength = int.MaxValue) : base(objective, searchSpace)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(epochLength);
        this.updatePolicy = updatePolicy;
        EpochClock = new EvaluationClock(epochLength);
        EnvironmentRandom = environmentRandom;
    }

    public EvaluationClock EpochClock { get; }
    protected IRandomNumberGenerator EnvironmentRandom { get; }

    public event EventHandler<IReadOnlyList<(TGenotype, ObjectiveVector, EvaluationTiming)>>? OnEvaluation;

    // this method will be called in parallel
    public override ObjectiveVector Evaluate(TGenotype genotype, IRandomNumberGenerator random)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        // PredictAndTrain in parallel read lock
        var timing = EpochClock.IncreaseCount();

        rwLock.EnterReadLock();
        ObjectiveVector r;
        try
        {
            r = Evaluate(genotype, random, timing);
        }
        finally
        {
            rwLock.ExitReadLock();
        }

        evaluationLog.Add((genotype, r, timing));

        if (updatePolicy == UpdatePolicy.Asynchronous)
        {
            ResolvePendingUpdates();
        }

        return r;
    }

    public abstract ObjectiveVector Evaluate(TGenotype solution, IRandomNumberGenerator random, EvaluationTiming timing);

    public void AfterEvaluation(IReadOnlyList<TGenotype> genotypes, IReadOnlyList<ObjectiveVector> objectiveVectors, TSearchSpace searchSpace, DynamicProblem<TGenotype, TSearchSpace> problem)
    {
        OnEvaluation?.Invoke(this, evaluationLog.OrderBy(x => x.timing.EpochCount).ToArray());
        evaluationLog.Clear();
        if (updatePolicy == UpdatePolicy.AfterEvaluation)
        {
            ResolvePendingUpdates();
        }
    }

    public void AfterInterception(ISearchState newState, ISearchState currentState, ISearchState? previousState, TSearchSpace searchSpace, DynamicProblem<TGenotype, TSearchSpace> problem)
    {
        if (updatePolicy == UpdatePolicy.AfterInterception)
        {
            ResolvePendingUpdates();
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposed)
            return;

        if (!disposing)
            return;

        disposed = true;
        rwLock.Dispose();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected abstract void Update();

    public void UpdateOnce()
    {
        EpochClock.AdvanceEpoch();
        ResolvePendingUpdates();
    }

    private void ResolvePendingUpdates()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (EpochClock.PendingEpochs == 0)
        {
            return; // pre-check
        }

        rwLock.EnterWriteLock();
        try
        {
            EpochClock.ResolvePendingEpochs(Update);
        }
        finally
        {
            rwLock.ExitWriteLock();
        }
    }
}
