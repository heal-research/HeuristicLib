using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms;

/// <remarks>
/// Says nothing about what a run yields, so <c>Stream</c> and <c>Complete</c> are not available here. Hold this form
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
    /// Resolution calls this for every build. Validate the configuration first and take shared execution data, such as
    /// counters or caches, from <see cref="ResolutionScope.GetScopedState{TState}"/>, passing this configuration.
    /// </remarks>
    IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutor<TSearchSpace, TProblem>(ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>;
}

public interface IAlgorithmExecutor<TCandidate, in TSearchSpace, in TProblem, TSearchState>
    : IExecutor
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    IAsyncEnumerable<TSearchState> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random, TSearchState? initialState = null, CancellationToken ct = default);
}

/// <summary>Advice that wraps every executor built for the algorithms its pointcut selects, such as observing each yielded state.</summary>
/// <remarks>
/// Data that must outlive one executor, such as a counter, belongs to the advice object; it is shared by every algorithm
/// and resolution the advice is applied to, including fresh activations inside a meta-algorithm.
/// </remarks>
public abstract class AlgorithmAdvice<TCandidate, TSearchState> : Advice<IAlgorithm<TCandidate, TSearchState>>, IAlgorithmAdvice
    where TSearchState : class, ISearchState
{
    /// <summary>Wraps one executor built for a selected algorithm and returns the executor its callers use.</summary>
    /// <param name="executor">The executor built for this resolution, possibly already wrapped by earlier advice.</param>
    /// <param name="source">The configuration the pointcut selected.</param>
    public abstract IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> Wrap<TSearchSpace, TProblem>(
        IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> executor, IAlgorithm<TCandidate, TSearchState> source)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>;
}

// Lets resolution skip advice for another search state without treating arbitrary Advice subclasses as supported.
internal interface IAlgorithmAdvice;

public static class AlgorithmResolutionExtensions
{
    extension<TCandidate, TSearchSpace, TProblem, TSearchState>(ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        public IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> Resolve(IAlgorithm<TCandidate> algorithm) =>
            scope.Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(algorithm);

        [return: NotNullIfNotNull(nameof(algorithm))]
        public IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>? ResolveOptional(IAlgorithm<TCandidate>? algorithm) =>
            algorithm is null ? null : scope.Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(algorithm);

        public bool TryResolve(
            IAlgorithm<TCandidate> algorithm,
            [NotNullWhen(true)] out IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>? executor,
            [NotNullWhen(false)] out string? reason) =>
            scope.TryResolve<TCandidate, TSearchSpace, TProblem, TSearchState>(algorithm, out executor, out reason);
    }
}
