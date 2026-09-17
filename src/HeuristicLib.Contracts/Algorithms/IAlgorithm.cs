using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms;

/// <remarks>
/// Says nothing about what a run yields, so <c>Stream</c> and <c>Complete</c> are not available here. Hold this form
/// to keep algorithms with different search states in one collection, and
/// <see cref="IAlgorithm{TCandidate, TSearchState}"/> to run one.
/// </remarks>
public interface IAlgorithm<TCandidate> : IExecutionInstanceResolvable
{
    IAlgorithmInstance<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> CreateExecutionInstance<TRunSearchSpace, TRunProblem, TRunSearchState>(ExecutionInstanceRegistry instanceRegistry)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
        where TRunSearchState : class, ISearchState;
}

/// <remarks>The search space and problem stay run supplied; only the search state is named here.</remarks>
public interface IAlgorithm<TCandidate, TSearchState> : IAlgorithm<TCandidate>
    where TSearchState : class, ISearchState
{
    IAlgorithmInstance<TCandidate, TRunSearchSpace, TRunProblem, TSearchState> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceRegistry instanceRegistry)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

public interface IAlgorithmInstance<TCandidate, in TSearchSpace, in TProblem, TSearchState>
    : IExecutionInstance
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    IAsyncEnumerable<TSearchState> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random, TSearchState? initialState = null, CancellationToken ct = default);
}

public static class AlgorithmResolverExtensions
{
    extension(ExecutionInstanceRegistry registry)
    {
        public IAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState> Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(IAlgorithm<TCandidate> algorithm)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
            where TSearchState : class, ISearchState =>
            registry.Resolve(algorithm, static (creationTarget, childRegistry) => creationTarget.CreateExecutionInstance<TSearchSpace, TProblem, TSearchState>(childRegistry));

        public IAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState>? ResolveOptional<TCandidate, TSearchSpace, TProblem, TSearchState>(IAlgorithm<TCandidate>? algorithm)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
            where TSearchState : class, ISearchState =>
            algorithm is null ? null : registry.Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(algorithm);

        /// <remarks>Reports rather than throws, so a caller can validate without catching.</remarks>
        public bool TryResolve<TCandidate, TSearchSpace, TProblem, TSearchState>(
            IAlgorithm<TCandidate> algorithm,
            [NotNullWhen(true)] out IAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState>? instance,
            [NotNullWhen(false)] out string? reason)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
            where TSearchState : class, ISearchState
        {
            try
            {
                instance = registry.Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(algorithm);
                reason = null;
                return true;
            }
            catch (InvalidOperationException exception)
            {
                instance = null;
                reason = exception.Message;
                return false;
            }
        }
    }

    extension<TCandidate, TSearchSpace, TProblem, TSearchState>(ExecutionInstanceResolver<TCandidate, TSearchSpace, TProblem, TSearchState> resolver)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        public IAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState> Resolve(IAlgorithm<TCandidate> algorithm) =>
            resolver.Registry.Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(algorithm);

        public IAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState>? ResolveOptional(IAlgorithm<TCandidate>? algorithm) =>
            algorithm is null ? null : resolver.Registry.Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(algorithm);

        public bool TryResolve(
            IAlgorithm<TCandidate> algorithm,
            [NotNullWhen(true)] out IAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState>? instance,
            [NotNullWhen(false)] out string? reason) =>
            resolver.Registry.TryResolve(algorithm, out instance, out reason);
    }
}
