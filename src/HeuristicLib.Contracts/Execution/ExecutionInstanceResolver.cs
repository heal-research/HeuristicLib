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
/// registry directly everywhere else. It is a readonly struct over a single reference, so building one costs nothing.
/// </remarks>
public readonly struct ExecutionInstanceResolver<TCandidate, TSearchSpace, TProblem>(ExecutionInstanceRegistry registry)
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public ExecutionInstanceRegistry Registry => registry;
}

/// <summary>Creates a typed resolver over a registry, naming the triple once per creation method.</summary>
public static class ExecutionInstanceRegistryResolverExtensions
{
    extension(ExecutionInstanceRegistry registry)
    {
        public ExecutionInstanceResolver<TCandidate, TSearchSpace, TProblem> For<TCandidate, TSearchSpace, TProblem>()
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> => new(registry);
    }
}
