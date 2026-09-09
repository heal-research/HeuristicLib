using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Execution;

/// <remarks>Convenience only: every overload forwards to the registry overload of the same name.</remarks>
public class ExecutionInstanceResolver<TCandidate, TSearchSpace, TProblem>(ExecutionInstanceRegistry registry)
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public ExecutionInstanceRegistry Registry => registry;
}

/// <summary>
/// A resolver that also names the search state, for an algorithm resolving terminators and interceptors alongside its
/// other operators.
/// </summary>
/// <remarks>
/// Terminators and interceptors name only their candidate, so the state they run over comes from the resolver rather
/// than from the call site.
/// </remarks>
public sealed class ExecutionInstanceResolver<TCandidate, TSearchSpace, TProblem, TSearchState>(ExecutionInstanceRegistry registry)
    : ExecutionInstanceResolver<TCandidate, TSearchSpace, TProblem>(registry)
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState;

/// <summary>Creates a typed resolver over a registry, naming the triple once per creation method.</summary>
public static class ExecutionInstanceRegistryResolverExtensions
{
    extension(ExecutionInstanceRegistry registry)
    {
        public ExecutionInstanceResolver<TCandidate, TSearchSpace, TProblem> For<TCandidate, TSearchSpace, TProblem>()
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> => new(registry);

        /// <summary>
        /// Names the search state as well, which an algorithm knows because it is the state the algorithm produces.
        /// The result still serves every role that binds on the triple alone.
        /// </summary>
        public ExecutionInstanceResolver<TCandidate, TSearchSpace, TProblem, TSearchState> For<TCandidate, TSearchSpace, TProblem, TSearchState>()
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
            where TSearchState : class, ISearchState => new(registry);
    }
}
