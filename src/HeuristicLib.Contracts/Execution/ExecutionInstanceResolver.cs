using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Execution;

/// <summary>
/// Names a candidate, search space and problem once, so a call site resolving several operators does not repeat the
/// triple on each of them.
/// </summary>
/// <remarks>
/// Convenience only, and never part of a contract: creation methods are handed the registry, and every resolver
/// overload forwards straight to the registry overload of the same name. Use it where it saves repetition and the
/// registry directly everywhere else.
/// <para>
/// It is a class rather than a struct so that
/// <see cref="ExecutionInstanceResolver{TCandidate, TSearchSpace, TProblem, TSearchState}"/> can derive from it. That
/// inheritance is what lets one resolver serve every role: extension members declared for this type apply to the
/// derived one too, so an algorithm that knows its search state resolves its state-aware operators and its other
/// seven through the same object. One small allocation per creation method is the price, and a creation method runs
/// once per run rather than once per iteration.
/// </para>
/// </remarks>
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
/// Terminators and interceptors name only their candidate, like every other role, so the state they run over has to
/// come from the call. Naming it as a method type argument is not expressible — an extension member declared in a
/// generic extension block merges the block's type parameters into its own, so an explicit type argument list must
/// supply all of them or none. Carrying the state on the resolver instead means nothing is named at the call site.
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
