using System.Collections.Concurrent;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Observers;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Problems.Dynamic;

public abstract class DynamicProblem<TGenotype, TSearchSpace> : IEvaluatorObserver<TGenotype>, IInterceptorObserver<TGenotype>,
                                                                IDynamicProblem<TGenotype, TSearchSpace>,
                                                                IDisposable
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TGenotype : class
{
  private readonly ConcurrentBag<(TGenotype solution, ObjectiveVector objective, EvaluationTiming timing)> evaluationLog = [];
  private readonly ReaderWriterLockSlim rwLock = new();

  public readonly UpdatePolicy UpdatePolicy;

  protected DynamicProblem(IRandomNumberGenerator environmentRandom, UpdatePolicy updatePolicy = UpdatePolicy.AfterEvaluation, int epochLength = int.MaxValue)
  {
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(epochLength);
    UpdatePolicy = updatePolicy;
    EpochClock = new EvaluationClock(epochLength);
    EnvironmentRandom = environmentRandom;
  }

  public EvaluationClock EpochClock { get; }
  protected IRandomNumberGenerator EnvironmentRandom { get; }

  public void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

  public abstract TSearchSpace SearchSpace { get; }
  public abstract Objective Objective { get; }

  public event EventHandler<IReadOnlyList<(TGenotype, ObjectiveVector, EvaluationTiming)>>? OnEvaluation;

  //this method will be called in parallel
  public ObjectiveVector Evaluate(TGenotype solution, IRandomNumberGenerator random)
  {
    //PredictAndTrain in parallel read lock
    var timing = EpochClock.IncreaseCount();

    rwLock.EnterReadLock();
    ObjectiveVector r;
    try {
      r = Evaluate(solution, random, timing);
    } finally { rwLock.ExitReadLock(); }

    evaluationLog.Add((solution, r, timing));

    if (UpdatePolicy == UpdatePolicy.Asynchronous) {
      ResolvePendingUpdates();
    }

    return r;
  }

  public abstract ObjectiveVector Evaluate(TGenotype solution, IRandomNumberGenerator random, EvaluationTiming timing);

  public void AfterEvaluation(IReadOnlyList<TGenotype> genotypes, IReadOnlyList<ObjectiveVector> values,
                              ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem)
  {
    OnEvaluation?.Invoke(this, evaluationLog.OrderBy(x => x.timing.EpochCount).ToArray());
    evaluationLog.Clear();
    if (UpdatePolicy == UpdatePolicy.AfterEvaluation) {
      ResolvePendingUpdates();
    }
  }

  public void AfterInterception(IAlgorithmState currentAlgorithmState, IAlgorithmState? previousIterationResult, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem)
  {
    if (UpdatePolicy == UpdatePolicy.AfterInterception) {
      ResolvePendingUpdates();
    }
  }

  public void AfterInterception(PopulationState<TGenotype> res, PopulationState<TGenotype>? previousState, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) => throw new NotImplementedException();

  ~DynamicProblem() => Dispose(false);

  protected virtual void Dispose(bool disposing) => rwLock.Dispose();

  protected abstract void Update();

  public void UpdateOnce()
  {
    EpochClock.AdvanceEpoch();
    ResolvePendingUpdates();
  }

  private void ResolvePendingUpdates()
  {
    if (EpochClock.PendingEpochs == 0) {
      return; //pre-check
    }

    rwLock.EnterWriteLock();
    try {
      EpochClock.ResolvePendingEpochs(Update);
    } finally { rwLock.ExitWriteLock(); }
  }
}
