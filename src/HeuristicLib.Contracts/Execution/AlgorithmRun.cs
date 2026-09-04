using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Execution;

public abstract class AlgorithmRun
{
    private readonly ImmutableArray<IExecutionHook> hooks;

    protected AlgorithmRun(IReadOnlyList<IExecutionHook> hooks)
    {
        this.hooks = [.. hooks];
    }

    public bool ExecutionStarted { get; private set; }

    protected ExecutionInstanceResolver StartExecution()
    {
        EnsureNotStarted();
        ExecutionStarted = true;

        // Installed in the order they were supplied, so the first installation at an anchor observes it first.
        return ExecutionInstanceResolver.Create(builder =>
        {
            foreach (var hook in hooks)
                builder.Install(hook);
        });
    }

    /// <summary>
    /// Completes everything installed that owns a resource or a completion state when the run ends.
    /// </summary>
    protected void DisposeHooks()
    {
        foreach (var hook in hooks)
        {
            if (hook is IDisposable disposable)
                disposable.Dispose();
        }
    }

    private void EnsureNotStarted()
    {
        if (ExecutionStarted)
        {
            throw new InvalidOperationException("A run can only be configured and executed once. Create a new run for another execution.");
        }
    }

}

public sealed class AlgorithmRun<TCandidate, TSearchSpace, TProblem, TSearchState> : AlgorithmRun
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    public IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> Algorithm { get; }

    public TProblem Problem { get; }

    public IRandomNumberGenerator Random { get; }

    public AlgorithmRun(IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> algorithm, TProblem problem, IRandomNumberGenerator random)
        : this(algorithm, problem, random, [])
    {
    }

    public AlgorithmRun(IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> algorithm, TProblem problem, IRandomNumberGenerator random, IReadOnlyList<IExecutionHook> hooks)
        : base(hooks)
    {
        Algorithm = algorithm;
        Problem = problem;
        Random = random;
    }

    public ExecutionStream<TSearchState> Stream(TSearchState? initialState = null, CancellationToken cancellationToken = default)
    {
        var algorithmInstance = StartExecution().Resolve(Algorithm);
        return new(StreamStates(algorithmInstance, initialState, cancellationToken), cancellationToken);
    }

    public async Task<TSearchState> CompleteAsync(TSearchState? initialState = null, CancellationToken cancellationToken = default) =>
        await Stream(initialState, cancellationToken).LastAsync(cancellationToken);

    public TSearchState Complete(TSearchState? initialState = null, CancellationToken cancellationToken = default) =>
        CompleteAsync(initialState, cancellationToken).GetAwaiter().GetResult();

    private async IAsyncEnumerable<TSearchState> StreamStates(IAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState> algorithmInstance, TSearchState? initialState, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var state in algorithmInstance.RunStreamingAsync(Problem, Random, initialState, cancellationToken))
            {
                yield return state;
            }
        }
        finally
        {
            DisposeHooks();
        }
    }
}
