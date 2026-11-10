using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Execution;

public abstract class AlgorithmRun
{
    private readonly Lock sync = new();
    private readonly ResolutionScope scope;
    private readonly IConfigurationNode configuration;

    public RunLifecycleState LifecycleState { get; private set; } = RunLifecycleState.Ready;

    protected AlgorithmRun(ResolutionScope scope, IConfigurationNode configuration)
    {
        this.scope = scope;
        this.configuration = configuration;
    }

    /// <summary>Returns this run's result of the instrument, creating it when the run has not looked it up yet.</summary>
    /// <remarks>
    /// The result can be read before, during and after execution, also after a failure. Each result type states how it
    /// may be read while the run writes to it, such as a trace through its snapshots.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// The instrument needs placing and the run's algorithm does not hold it, so it would read empty. What a delegate
    /// creates cannot be seen, so nothing is reported once the algorithm holds a delegate.
    /// </exception>
    public TResult Result<TResult>(Instrument<TResult> instrument) where TResult : class
    {
        RequireHeld(instrument);
        return scope.Results.Result(instrument);
    }

    /// <summary>Returns this run's result of the configuration, such as the accumulator of a counter.</summary>
    /// <remarks>It can be read before, during and after execution, also after a failure.</remarks>
    /// <exception cref="InvalidOperationException">
    /// The configuration holds substitutions that place it, as a counter created from a source does, and the run's
    /// algorithm does not hold it, so it would read empty. Nothing is reported once the algorithm holds a delegate.
    /// </exception>
    public TResult Result<TResult>(IConfigurationNode<TResult> configuration) where TResult : class, IRunResult
    {
        if (configuration is ISubstitutionBundle bundle)
            RequireHeld(bundle);
        return scope.RunResult(configuration);
    }

    private void RequireHeld(ISubstitutionBundle bundle)
    {
        if (bundle.Substitutions.Count > 0 && !MayHold(configuration, bundle))
            throw new InvalidOperationException($"This run's algorithm does not hold the {ExecutionSignature.Name(bundle.GetType())}, so its result would stay empty. " +
                "Attach it with algorithm.AnalyzedBy(analyzer) and create the run from the configuration that call returns.");
    }

    // Whether a declared parameter reachable from the root holds the value, or a delegate could have placed it unseen.
    private static bool MayHold(IConfigurationNode root, object value)
    {
        var seen = new HashSet<IConfigurationNode>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<IConfigurationNode>([root]);
        while (pending.TryPop(out var node))
        {
            if (!seen.Add(node))
                continue;
            foreach (var parameter in node.GetParameters())
            {
                var held = parameter.GetValue(node);
                if (ReferenceEquals(held, value) || held is Delegate)
                    return true;
                foreach (var child in parameter.GetChildren(node))
                    pending.Push(child);
            }
        }
        return false;
    }

    protected ResolutionScope? BeginExecutionSegment()
    {
        lock (sync)
        {
            if (LifecycleState == RunLifecycleState.Completed)
                return null;
            if (LifecycleState == RunLifecycleState.Running)
                throw new InvalidOperationException("This run already has an active execution stream.");
            if (LifecycleState is not (RunLifecycleState.Ready or RunLifecycleState.Paused))
                throw new InvalidOperationException($"A run cannot continue while its lifecycle state is {LifecycleState}.");

            LifecycleState = RunLifecycleState.Running;
            return scope;
        }
    }

    protected void ExecutionCompleted() => TransitionFromRunning(RunLifecycleState.Completed);

    protected void ExecutionCanceled()
    {
        lock (sync)
        {
            if (LifecycleState is RunLifecycleState.Running or RunLifecycleState.Paused)
                LifecycleState = RunLifecycleState.Canceled;
        }
    }

    protected void ExecutionFailed() => TransitionFromRunning(RunLifecycleState.Failed);

    protected void ExecutionPaused() => TransitionFromRunning(RunLifecycleState.Paused);

    private void TransitionFromRunning(RunLifecycleState next)
    {
        lock (sync)
        {
            if (LifecycleState == RunLifecycleState.Running)
                LifecycleState = next;
        }
    }
}

public sealed class AlgorithmRun<TCandidate, TSearchSpace, TProblem, TSearchState> : AlgorithmRun
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    private IAsyncEnumerator<TSearchState>? execution;
    public IAlgorithm<TCandidate, TSearchState> Algorithm { get; }

    public TProblem Problem { get; }

    public IRandomNumberGenerator Random { get; }

    public AlgorithmRun(IAlgorithm<TCandidate, TSearchState> algorithm, TProblem problem, IRandomNumberGenerator random)
        : base(ResolutionScope.Create(), algorithm)
    {
        Algorithm = algorithm;
        Problem = problem;
        Random = random;
    }

    public ExecutionStream<TSearchState> Stream(TSearchState? initialState = null, CancellationToken cancellationToken = default) =>
        CreateStream(initialState, cancellationToken, cancellationTerminatesRun: false);

    internal ExecutionStream<TSearchState> StreamForTerminalCancellation(
        TSearchState? initialState, CancellationToken cancellationToken) =>
        CreateStream(initialState, cancellationToken, cancellationTerminatesRun: true);

    private ExecutionStream<TSearchState> CreateStream(TSearchState? initialState, CancellationToken cancellationToken,
        bool cancellationTerminatesRun)
    {
        var scope = BeginExecutionSegment();
        if (scope is null)
            return new(AsyncEnumerable.Empty<TSearchState>());

        try
        {
            if (execution is null)
            {
                var algorithmExecutor = scope.Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(Algorithm);
                var executionCancellation = cancellationTerminatesRun ? cancellationToken : CancellationToken.None;
                execution = algorithmExecutor.RunStreamingAsync(Problem, Random, initialState, executionCancellation)
                                             .GetAsyncEnumerator(executionCancellation);
            }
            else if (initialState is not null)
            {
                ExecutionPaused();
                throw new InvalidOperationException("An initial state can only be supplied when a run starts.");
            }

            return new(Track(cancellationToken, cancellationTerminatesRun), cancellationToken);
        }
        catch (OperationCanceledException)
        {
            ExecutionCanceled();
            throw;
        }
        catch
        {
            ExecutionFailed();
            throw;
        }
    }

    public async Task<TSearchState> CompleteAsync(TSearchState? initialState = null, CancellationToken cancellationToken = default) =>
        await Stream(initialState, cancellationToken).LastAsync(cancellationToken);

    public TSearchState Complete(TSearchState? initialState = null, CancellationToken cancellationToken = default) =>
        CompleteAsync(initialState, cancellationToken).GetAwaiter().GetResult();

    internal async ValueTask CancelAsync()
    {
        var currentExecution = execution;
        execution = null;
        ExecutionCanceled();
        if (currentExecution is not null)
            await currentExecution.DisposeAsync();
    }

    private async IAsyncEnumerable<TSearchState> Track(
        CancellationToken streamCancellation,
        bool cancellationTerminatesRun,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken enumerationCancellation = default)
    {
        using var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(
            streamCancellation, enumerationCancellation);
        var cancellationToken = cancellationSource.Token;
        var ended = false;
        try
        {
            while (true)
            {
                bool hasNext;
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    hasNext = await execution!.MoveNextAsync();
                }
                catch (OperationCanceledException)
                {
                    if (cancellationTerminatesRun)
                        ExecutionCanceled();
                    else
                        ExecutionPaused();
                    throw;
                }
                catch
                {
                    ExecutionFailed();
                    throw;
                }

                if (!hasNext)
                {
                    ended = true;
                    var completedExecution = execution;
                    execution = null;
                    try
                    {
                        await completedExecution.DisposeAsync();
                    }
                    catch
                    {
                        ExecutionFailed();
                        throw;
                    }
                    ExecutionCompleted();
                    yield break;
                }

                yield return execution.Current;
            }
        }
        finally
        {
            if (!ended && !cancellationTerminatesRun)
                ExecutionPaused();
        }
    }

}
