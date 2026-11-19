using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms;

/// <remarks>
/// Says nothing about what a run yields, so <c>RunStreaming</c> and <c>RunToCompletion</c> are not available here. Hold this form
/// to keep algorithms with different search states in one collection, and
/// <see cref="IAlgorithm{TCandidate, TSearchState}"/> to run one. Resolving it for a search state checks that it
/// produces that state.
/// </remarks>
public interface IAlgorithm<TCandidate> : IConfigurationNode;

/// <remarks>The search space and problem stay run supplied; only the search state is named here.</remarks>
public interface IAlgorithm<TCandidate, TSearchState> : IAlgorithm<TCandidate>
    where TSearchState : class, ISearchState
{
    /// <summary>Builds this algorithm's executor for the run types, resolving children through the scope.</summary>
    /// <remarks>
    /// Resolution calls this once per configuration and problem in a scope, on the first request; later requests for that
    /// pair receive the same executor. Validate configuration first. Obtain shared resources through explicit handles when sharing is intended.
    /// Keep private mutable data in executor fields.
    /// </remarks>
    IAlgorithmExecutor<TSearchState> CreateExecutor<TSearchSpace, TProblem>(ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>;
}

public interface IAlgorithmExecutor<TSearchState>
    : IExecutor
    where TSearchState : class, ISearchState
{
    IAsyncEnumerable<TSearchState> RunStreamingAsync(IRandomNumberGenerator random, TSearchState? initialState = null, CancellationToken ct = default);
}

public static class AlgorithmResolutionExtensions
{
    extension<TCandidate, TSearchSpace, TProblem, TSearchState>(ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        public IAlgorithmExecutor<TSearchState> Resolve(IAlgorithm<TCandidate> algorithm) =>
            scope.Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(algorithm, scope.Problem);

        [return: NotNullIfNotNull(nameof(algorithm))]
        public IAlgorithmExecutor<TSearchState>? ResolveOptional(IAlgorithm<TCandidate>? algorithm) =>
            algorithm is null ? null : scope.Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(algorithm, scope.Problem);

        public bool TryResolve(
            IAlgorithm<TCandidate> algorithm,
            [NotNullWhen(true)] out IAlgorithmExecutor<TSearchState>? executor,
            [NotNullWhen(false)] out string? reason) =>
            scope.TryResolve<TCandidate, TSearchSpace, TProblem, TSearchState>(algorithm, scope.Problem, out executor, out reason);
    }
}
