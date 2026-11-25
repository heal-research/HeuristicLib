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

    /// <summary>Returns this run's result of the configuration, such as the accumulator of a counter.</summary>
    /// <remarks>
    /// Creates the root scope's result on first lookup. It can be read before, during and after execution, also after
    /// a failure; the result type defines whether live reads are safe. Analyzer results span every Cycle turn.
    /// </remarks>
    /// <param name="configuration">The result-producing configuration, identified by reference.</param>
    /// <exception cref="InvalidOperationException">
    /// The configuration holds substitutions that place it, and the run's
    /// algorithm provably does not hold it, so it would read empty; or result construction recursively requests itself.
    /// A reachable declared delegate prevents proving absence, so the placement check permits it.
    /// </exception>
    public TResult Result<TResult>(IConfigurationNode<TResult> configuration) where TResult : class, IRunResult
    {
        RequireHeld(configuration);
        return scope.RunResult(configuration);
    }

    internal void RequireHeld(IConfigurationNode node)
    {
        if (node is ISubstitution substitution && substitution.Expand().Count > 0 && !MayHold(configuration, node))
            throw new InvalidOperationException($"This run's algorithm does not hold the {ExecutionSignature.Name(node.GetType())}, so its result would stay empty. " +
                "Place it with algorithm.CreateRun(problem, random, [substitution]), or with Substitute for a configuration you keep, " +
                "and check that the configuration holds what it observes, such as an evaluator of its candidate type.");
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

    /// <summary>Creates a ready run without placing substitutions or validating configuration contracts.</summary>
    /// <remarks>The executor graph is resolved on first start. Use <c>algorithm.CreateRun(...)</c> for validation and substitution placement.</remarks>
    public AlgorithmRun(IAlgorithm<TCandidate, TSearchState> algorithm, TProblem problem, IRandomNumberGenerator random)
        : this(algorithm, problem, random, TimeProvider.System)
    {
    }

    internal AlgorithmRun(IAlgorithm<TCandidate, TSearchState> algorithm, TProblem problem, IRandomNumberGenerator random,
        TimeProvider timeProvider)
        : base(ResolutionScope.Create(timeProvider), algorithm)
    {
        Algorithm = algorithm;
        Problem = problem;
        Random = random;
    }

    /// <summary>Starts or resumes the retained execution with a single-consumer stream.</summary>
    /// <param name="initialState">An optional starting state, permitted only before execution first starts.</param>
    /// <param name="cancellationToken">Cancels this stream at root yield boundaries, pausing the run for later resumption.</param>
    /// <remarks>Stopping consumption pauses the run. Completion and failure are terminal; results remain readable.</remarks>
    /// <exception cref="InvalidOperationException">A stream is active, the run is canceled or failed, or a resumed run receives an initial state.</exception>
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

        if (execution is { } current)
        {
            if (initialState is not null)
            {
                ExecutionPaused();
                throw new InvalidOperationException("An initial state can only be supplied when a run starts.");
            }

            return new(Track(_ => current, scope.Results, cancellationToken, cancellationTerminatesRun), cancellationToken);
        }

        try
        {
            var algorithmExecutor = scope.Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(Algorithm, Problem);
            if (cancellationTerminatesRun)
                return new(Track(StartIterator, scope.Results, cancellationToken, cancellationTerminatesRun), cancellationToken);

            var iterator = StartIterator(CancellationToken.None);
            return new(Track(_ => iterator, scope.Results, cancellationToken, cancellationTerminatesRun), cancellationToken);

            IAsyncEnumerator<TSearchState> StartIterator(CancellationToken token) =>
                execution = algorithmExecutor.RunStreamingAsync(Random, initialState, token).GetAsyncEnumerator(token);
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

    /// <summary>Consumes a new execution stream to its last state, resuming a paused run if necessary.</summary>
    /// <remarks>Cancellation pauses the run as for <see cref="Stream"/>. An execution yielding no state has no last state.</remarks>
    public async Task<TSearchState> CompleteAsync(TSearchState? initialState = null, CancellationToken cancellationToken = default) =>
        await Stream(initialState, cancellationToken).LastAsync(cancellationToken);

    /// <summary>Blocks while <see cref="CompleteAsync"/> consumes execution to its last state.</summary>
    public TSearchState Complete(TSearchState? initialState = null, CancellationToken cancellationToken = default) =>
        CompleteAsync(initialState, cancellationToken).GetAwaiter().GetResult();

    internal async ValueTask CancelAsync()
    {
        ExecutionCanceled();
        await DisposeIteratorAsync();
    }

    private async ValueTask DisposeIteratorAsync()
    {
        var currentExecution = execution;
        execution = null;
        if (currentExecution is not null)
            await currentExecution.DisposeAsync();
    }

    private async IAsyncEnumerable<TSearchState> Track(
        Func<CancellationToken, IAsyncEnumerator<TSearchState>> getIterator,
        RunResults results,
        CancellationToken streamCancellation,
        bool cancellationTerminatesRun,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken enumerationCancellation = default)
    {
        try
        {
            using var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(streamCancellation, enumerationCancellation);
            var cancellationToken = cancellationSource.Token;
            IAsyncEnumerator<TSearchState> iterator;
            try
            {
                if (cancellationTerminatesRun)
                    cancellationToken.ThrowIfCancellationRequested();
                iterator = getIterator(cancellationToken);
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

            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    if (cancellationTerminatesRun)
                        ExecutionCanceled();
                    cancellationToken.ThrowIfCancellationRequested();
                }

                TSearchState state;
                try
                {
                    bool hasNext;
                    results.EnterCall();
                    try
                    {
                        hasNext = await iterator.MoveNextAsync();
                    }
                    finally
                    {
                        results.ExitCall();
                    }

                    if (!hasNext)
                        break;
                    state = iterator.Current;
                }
                catch (Exception exception)
                {
                    if (cancellationTerminatesRun && exception is OperationCanceledException)
                        ExecutionCanceled();
                    else
                        ExecutionFailed();
                    await DisposeIteratorAsync().AsTask().ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
                    throw;
                }

                yield return state;
            }

            try
            {
                await DisposeIteratorAsync();
            }
            catch
            {
                ExecutionFailed();
                throw;
            }
            ExecutionCompleted();
        }
        finally
        {
            if (!cancellationTerminatesRun)
                ExecutionPaused();
        }
    }

}
