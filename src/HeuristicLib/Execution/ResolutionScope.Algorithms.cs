using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Execution;

public partial class ResolutionScope
{
    /// <summary>Builds an algorithm's executor for the run's types, or returns the one this scope already built for it.</summary>
    /// <remarks>
    /// The algorithm and its declared children must support the run, and the algorithm must produce the requested search state. A
    /// mismatch names every configuration responsible, such as an operator written for another search space. An algorithm
    /// resolved at the root scope outside any construction is the run's root: the time spent advancing its executor counts
    /// toward <see cref="RunResults.Elapsed"/>.
    /// </remarks>
    public IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(IAlgorithm<TCandidate> source)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        node.Tree.ThrowIfFailed();
        if (source is not IAlgorithm<TCandidate, TSearchState> algorithm)
            throw new InvalidOperationException($"{ExecutionSignature.Name(source.GetType())} produces {ProducedSearchStates(source)}, " +
                $"and cannot run producing {ExecutionSignature.Name(typeof(TSearchState))}.");
        var execution = ExecutionSignature.For<TSearchSpace, TProblem, TSearchState>();
        if (!execution.Supports(algorithm))
            throw SearchConfigurationValidation.Mismatch(algorithm, execution);
        using var building = node.Tree.Enter(algorithm);
        var root = node.Parent is null && node.Tree.EnteredOutsideConstruction;
        var owner = Owner(algorithm);
        var existing = Existing(owner, algorithm, static executor => executor is IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>);
        node.MarkResolved(algorithm);
        try
        {
            // The resolver is the one place that constructs executors; the delegate only chooses the constructing scope.
#pragma warning disable HLib0001
            var executor = (IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>?)existing
                ?? Build(owner, algorithm, scope => algorithm.CreateExecutor(scope.For<TCandidate, TSearchSpace, TProblem, TSearchState>()));
#pragma warning restore HLib0001
            return root ? new ElapsedTimeExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>(executor, node.Tree.Results) : executor;
        }
        catch
        {
            node.Tree.Fail(algorithm);
            throw;
        }
    }

    [return: NotNullIfNotNull(nameof(algorithm))]
    public IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>? ResolveOptional<TCandidate, TSearchSpace, TProblem, TSearchState>(IAlgorithm<TCandidate>? algorithm)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState =>
        algorithm is null ? null : Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(algorithm);

    /// <inheritdoc cref="TryResolve{TExecutor}"/>
    public bool TryResolve<TCandidate, TSearchSpace, TProblem, TSearchState>(IAlgorithm<TCandidate> algorithm,
        [NotNullWhen(true)] out IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>? executor, [NotNullWhen(false)] out string? reason)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState =>
        Attempt(() => Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(algorithm), out executor, out reason);

    private static string ProducedSearchStates(IConfigurationNode source) =>
        string.Join(" or ", source.GetType().GetInterfaces()
            .Where(contract => contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IAlgorithm<,>))
            .Select(contract => ExecutionSignature.Name(contract.GetGenericArguments()[1]))
            .DefaultIfEmpty("no search state"));
}

// Adds the time spent inside each call that advances the root algorithm, including its wrappers, to the run's elapsed
// time. Everything else passes through unchanged, so the stream completes, fails and is disposed exactly as the wrapped
// one is, and the wrapped executor's controls stay reachable.
file sealed class ElapsedTimeExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>(
    IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> executor, RunResults results)
    : IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>, IExecutorControlProvider
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    public IAsyncEnumerable<TSearchState> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random,
        TSearchState? initialState = null, CancellationToken ct = default) =>
        new TimedStates(executor.RunStreamingAsync(problem, random, initialState, ct), results);

    public TControl? Get<TControl>() where TControl : class => ExecutorControls.Find<TControl>(executor);

    private sealed class TimedStates(IAsyncEnumerable<TSearchState> states, RunResults results) : IAsyncEnumerable<TSearchState>
    {
        public IAsyncEnumerator<TSearchState> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
            new Enumerator(states.GetAsyncEnumerator(cancellationToken), results);
    }

    private sealed class Enumerator(IAsyncEnumerator<TSearchState> states, RunResults results) : IAsyncEnumerator<TSearchState>
    {
        public TSearchState Current => states.Current;

        public async ValueTask<bool> MoveNextAsync()
        {
            results.EnterCall();
            try
            {
                return await states.MoveNextAsync();
            }
            finally
            {
                results.ExitCall();
            }
        }

        public ValueTask DisposeAsync() => states.DisposeAsync();
    }
}
