using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Execution;

public abstract class AlgorithmRun
{
    private readonly Lock sync = new();
    private readonly List<IAnalyzer> analyzers = [];
    private readonly HashSet<IAnalyzer> analyzerSet = new(ReferenceEqualityComparer.Instance);
    private readonly List<IExecutionModule> modules = [];
    private readonly HashSet<IExecutionModule> moduleSet = new(ReferenceEqualityComparer.Instance);
    private ResolutionScope? scope;

    public RunLifecycleState LifecycleState { get; private set; } = RunLifecycleState.Preparing;

    protected AlgorithmRun()
    {
    }

    protected void Add(IAnalyzer analyzer)
    {
        lock (sync)
        {
            EnsurePreparing();
            if (analyzerSet.Add(analyzer))
                analyzers.Add(analyzer);
        }
    }

    protected void Add(IExecutionModule module)
    {
        lock (sync)
        {
            EnsurePreparing();
            if (moduleSet.Add(module))
                modules.Add(module);
        }
    }

    protected ResolutionScope? BeginExecutionSegment()
    {
        lock (sync)
        {
            if (LifecycleState == RunLifecycleState.Completed)
                return null;
            if (LifecycleState == RunLifecycleState.Running)
                throw new InvalidOperationException("This run already has an active execution stream.");
            if (LifecycleState is not (RunLifecycleState.Preparing or RunLifecycleState.Paused))
                throw new InvalidOperationException($"A run cannot continue while its lifecycle state is {LifecycleState}.");

            if (LifecycleState == RunLifecycleState.Paused)
            {
                LifecycleState = RunLifecycleState.Running;
                return scope!;
            }

            LifecycleState = RunLifecycleState.Running;

            try
            {
                scope = ResolutionScope.Create(builder =>
                {
                    foreach (var analyzer in analyzers)
                        analyzer.Install(builder);
                    foreach (var module in modules)
                        builder.Install(module);
                });
                return scope;
            }
            catch (OperationCanceledException)
            {
                LifecycleState = RunLifecycleState.Canceled;
                throw;
            }
            catch
            {
                LifecycleState = RunLifecycleState.Failed;
                throw;
            }
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

    private void EnsurePreparing()
    {
        if (LifecycleState != RunLifecycleState.Preparing)
        {
            throw new InvalidOperationException($"A run only accepts configuration and can only start while it is {RunLifecycleState.Preparing}; its current lifecycle state is {LifecycleState}.");
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
    {
        Algorithm = algorithm;
        Problem = problem;
        Random = random;
    }

    public AlgorithmRun<TCandidate, TSearchSpace, TProblem, TSearchState> AddAnalyzer(IAnalyzer analyzer)
    {
        Add(analyzer);
        return this;
    }

    public AlgorithmRun<TCandidate, TSearchSpace, TProblem, TSearchState> AddExecutionModule(IExecutionModule module)
    {
        Add(module);
        return this;
    }

    public ExecutionStream<TSearchState> Stream(TSearchState? initialState = null, CancellationToken cancellationToken = default) =>
        CreateStream(initialState, cancellationToken, cancellationTerminatesRun: false);

    internal ExecutionStream<TSearchState> StreamForTerminalCancellation(
        TSearchState? initialState, CancellationToken cancellationToken) =>
        CreateStream(initialState, cancellationToken, cancellationTerminatesRun: true);

    private ExecutionStream<TSearchState> CreateStream(TSearchState? initialState, CancellationToken cancellationToken,
        bool cancellationTerminatesRun)
    {
        try
        {
            var scope = BeginExecutionSegment();
            if (scope is null)
                return new(AsyncEnumerable.Empty<TSearchState>());

            if (execution is null)
            {
                var algorithmInstance = scope.Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(Algorithm);
                var executionCancellation = cancellationTerminatesRun ? cancellationToken : CancellationToken.None;
                execution = algorithmInstance.RunStreamingAsync(Problem, Random, initialState, executionCancellation)
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
        if (currentExecution is not null)
            await currentExecution.DisposeAsync();
        ExecutionCanceled();
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
                    await execution!.DisposeAsync();
                    execution = null;
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
