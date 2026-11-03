using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Execution;

public abstract class AlgorithmRun
{
    private readonly Lock sync = new();
    private readonly ResolutionScope scope;

    public RunLifecycleState LifecycleState { get; private set; } = RunLifecycleState.Ready;

    protected AlgorithmRun(ResolutionScope scope)
    {
        this.scope = scope;
    }

    /// <summary>Returns this run's resource, creating its default instance lazily when it has not been requested yet.</summary>
    /// <remarks>Supplied resources remain caller owned. The returned resource defines how concurrent reads and writes are supported.</remarks>
    public TResource Resource<TResource>(ResourceHandle<TResource> handle) where TResource : class => scope.Resource(handle);

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

    public AlgorithmRun(IAlgorithm<TCandidate, TSearchState> algorithm, TProblem problem, IRandomNumberGenerator random,
        Action<AlgorithmRunSetup<TCandidate, TSearchState>>? setup = null)
        : this(algorithm, problem, random, CreateScope(algorithm, setup))
    {
    }

    internal AlgorithmRun(IAlgorithm<TCandidate, TSearchState> algorithm, TProblem problem, IRandomNumberGenerator random, ResolutionScope scope)
        : base(scope)
    {
        Algorithm = algorithm;
        Problem = problem;
        Random = random;
    }

    private static ResolutionScope CreateScope(IAlgorithm<TCandidate, TSearchState> algorithm,
        Action<AlgorithmRunSetup<TCandidate, TSearchState>>? configure)
    {
        var setup = new AlgorithmRunSetup<TCandidate, TSearchState>(algorithm);
        configure?.Invoke(setup);
        return setup.BuildScope();
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
