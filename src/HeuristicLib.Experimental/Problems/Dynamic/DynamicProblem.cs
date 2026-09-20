using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.Dynamic;

/// <summary>
/// A problem whose environment changes while the run searches it.
/// </summary>
/// <remarks>
/// The environment advances in epochs counted in evaluations, and <see cref="UpdatePolicy"/> decides how much work
/// finishes before a due update is applied. Batching a problem's own evaluation is what gives it those boundaries, which
/// is why this does not inherit the batching of <see cref="SingleSolutionProblem{TSelf, TCandidate, TSearchSpace}"/>. One
/// evaluation is always scored by exactly one environment: an update waits behind the evaluations in flight, and the
/// ones that follow wait behind it.
/// </remarks>
public abstract class DynamicProblem<TSelf, TCandidate, TSearchSpace> :
    Problem<TSelf, TCandidate, TSearchSpace>,
    IDynamicProblem<TCandidate, TSearchSpace>,
    IUpdateRequestable,
    IDisposable
    where TSelf : Problem<TSelf, TCandidate, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    private readonly ReaderWriterLockSlim rwLock = new();
    private readonly Lock epochLock = new();
    private readonly UpdatePolicy updatePolicy;
    private int pendingUpdates;
    private bool disposed;

    protected DynamicProblem(ObjectiveDirections objective, TSearchSpace searchSpace, IRandomNumberGenerator environmentRandom, IEpochSchedule epochSchedule, UpdatePolicy updatePolicy = UpdatePolicy.AfterEachEvaluation) : base(objective, searchSpace)
    {
        this.updatePolicy = updatePolicy;
        EpochSchedule = epochSchedule;
        EnvironmentRandom = environmentRandom;
    }

    public ExecutionConcurrency Concurrency { get; init; } = ExecutionConcurrency.Sequential();

    public IEpochSchedule EpochSchedule { get; }

    /// <summary>
    /// The environment version currently in effect, which is the number of updates that have been applied.
    /// </summary>
    public int CurrentEpoch { get; private set; }

    /// <summary>
    /// Raised once an update has been applied, with the environment version now in effect.
    /// </summary>
    public event EventHandler<int>? OnEpochChange;

    protected IRandomNumberGenerator EnvironmentRandom { get; }

    /// <summary>
    /// Evaluates one batch of candidates.
    /// </summary>
    public sealed override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        // Reads as the wrong end, but this is what "after each batch" means: the update is postponed past the batch
        // and everything following it, and here is the last moment it can still be postponed to. Applying it here
        // rather than when the previous batch ended leaves that batch readable against the environment that scored it.
        ApplyUpdatesDueAt(UpdatePolicy.AfterEachBatchEvaluation);

        return BatchExecution.Execute(candidates, Evaluate, random, Concurrency);
    }

    /// <summary>
    /// Evaluates one candidate of a batch. Private, because evaluating outside a batch would skip the boundaries the
    /// update policy applies its updates at.
    /// </summary>
    // this method will be called in parallel
    private ObjectiveVector Evaluate(TCandidate candidate, IRandomNumberGenerator random)
    {
        ApplyUpdatesDueAt(UpdatePolicy.AfterEachEvaluation);

        // Counted under the read lock, so the recorded environment version is the one that scores this candidate. An
        // update taking the write lock cannot interleave between counting and scoring.
        rwLock.EnterReadLock();
        try
        {
            return Evaluate(candidate, random, ReportEvaluation());
        }
        finally { rwLock.ExitReadLock(); }
    }

    /// <summary>
    /// Asks the schedule whether an epoch has ended, and applies what is due when this boundary is the one the update
    /// policy waits for.
    /// </summary>
    /// <remarks>
    /// Asking here rather than only where work is reported is what lets a schedule end an epoch on something other than
    /// the work itself, such as elapsed time: nothing has to be evaluated for the problem to notice.
    /// </remarks>
    private void ApplyUpdatesDueAt(UpdatePolicy boundary)
    {
        lock (epochLock)
        {
            TakeDueEpochs();
        }

        if (updatePolicy == boundary)
        {
            ApplyPendingUpdates();
        }
    }

    /// <summary>
    /// Turns the epochs the schedule says have ended into pending updates.
    /// </summary>
    private void TakeDueEpochs() => pendingUpdates += EpochSchedule.TakeDueEpochs();

    /// <summary>
    /// Scores one candidate against the environment version <paramref name="epoch"/> names. Protected, because calling
    /// it from outside would score a candidate the epoch schedule never counted.
    /// </summary>
    protected abstract ObjectiveVector Evaluate(TCandidate candidate, IRandomNumberGenerator random, int epoch);

    /// <summary>
    /// Creates the hook that applies deferred updates at an algorithm's iteration boundary. Pass it to run creation.
    /// </summary>
    /// <remarks>
    /// Only <see cref="UpdatePolicy.AfterEachIteration"/> needs this, because an iteration belongs to an algorithm
    /// rather than to the problem. Every other policy needs nothing installed.
    /// </remarks>
    public IExecutionHook CreateIterationUpdateHook<TSearchState>(IAlgorithm<TCandidate, TSearchState> algorithm)
        where TSearchState : class, ISearchState =>
        new IterationUpdateHook<TSelf, TCandidate, TSearchSpace, TSearchState>(this, algorithm);

    internal void CompleteIteration()
    {
        if (updatePolicy == UpdatePolicy.AfterEachIteration)
        {
            ApplyPendingUpdates();
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

    /// <summary>
    /// Makes an update due regardless of what the schedule would have decided, and starts a new epoch.
    /// </summary>
    /// <remarks>
    /// The update policy still decides when it is applied. Use <see cref="UpdateOnce"/> to advance and apply in one go.
    /// </remarks>
    internal void RequestUpdate()
    {
        lock (epochLock)
        {
            pendingUpdates++;
            EpochSchedule.Restart();
        }
    }

    void IUpdateRequestable.RequestUpdate() => RequestUpdate();


    /// <summary>
    /// Applies the updates that are due, once each, and raises <see cref="OnEpochChange"/> if any were.
    /// </summary>
    internal void ApplyPendingUpdates()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (pendingUpdates == 0)
        {
            return; // pre-check
        }

        rwLock.EnterWriteLock();
        try
        {
            int applied;
            int reached;
            lock (epochLock)
            {
                applied = pendingUpdates;
                while (pendingUpdates > 0)
                {
                    Update();
                    pendingUpdates--;
                    CurrentEpoch++;
                }

                reached = CurrentEpoch;
            }

            // One event per environment entered, not per resolution: a schedule can owe several at once, and an
            // environment nothing was evaluated against is still an environment this run passed through.
            for (var epoch = reached - applied + 1; epoch <= reached; epoch++)
            {
                OnEpochChange?.Invoke(this, epoch);
            }
        }
        finally { rwLock.ExitWriteLock(); }
    }

    /// <summary>
    /// Advances the environment and applies the update immediately, bypassing the schedule.
    /// </summary>
    /// <remarks>
    /// Internal, because when the environment advances is the schedule's decision. This exists for driving a problem
    /// deterministically in a test, which the schedule cannot express: it is asked whether an epoch has ended only when
    /// work is reported, so it has no way to say "now" while nothing is being evaluated.
    /// </remarks>
    internal void UpdateOnce()
    {
        RequestUpdate();
        ApplyPendingUpdates();
    }

    /// <summary>
    /// Reports one evaluation to the schedule and returns the environment version scoring it.
    /// </summary>
    private int ReportEvaluation()
    {
        lock (epochLock)
        {
            EpochSchedule.RecordProgress();
            TakeDueEpochs();

            return CurrentEpoch;
        }
    }
}

/// <summary>
/// Lets this assembly make an update due on a dynamic problem it knows only through <see cref="IDynamicProblem{TCandidate, TSearchSpace}"/>.
/// </summary>
/// <remarks>
/// Internal for the reason <c>RequestUpdate</c> is: when the environment advances is the schedule's decision.
/// </remarks>
internal interface IUpdateRequestable
{
    void RequestUpdate();
}

/// <summary>
/// Applies a dynamic problem's deferred updates at an algorithm's iteration boundary.
/// </summary>
internal sealed class IterationUpdateHook<TSelf, TCandidate, TSearchSpace, TSearchState>(
    DynamicProblem<TSelf, TCandidate, TSearchSpace> problem,
    IAlgorithm<TCandidate, TSearchState> algorithm)
    : IExecutionHook
    where TSelf : Problem<TSelf, TCandidate, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TSearchState : class, ISearchState
{
    public void Install(ExecutionInstanceResolverBuilder builder) =>
        builder.Observe<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, TSearchState>(algorithm, _ => problem.CompleteIteration());
}
