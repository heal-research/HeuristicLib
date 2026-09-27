using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Execution;

/// <summary>
/// A scope that names the candidate, search space and problem once, so that a role binding on that triple does not
/// name them again at every call.
/// </summary>
/// <remarks>Convenience only: every overload forwards to the scope overload of the same name.</remarks>
public class ResolutionScope<TCandidate, TSearchSpace, TProblem>(ResolutionScope scope)
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public ResolutionScope Scope => scope;
}

/// <summary>
/// A scope that also names the search state, for an algorithm resolving terminators and interceptors alongside its
/// other operators.
/// </summary>
/// <remarks>
/// Terminators and interceptors name only their candidate, so the state they run over comes from the scope rather
/// than from the call site.
/// </remarks>
public sealed class ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState>(ResolutionScope scope)
    : ResolutionScope<TCandidate, TSearchSpace, TProblem>(scope)
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState;

/// <summary>Creates a typed scope over a resolution scope, naming the triple once per creation method.</summary>
public static class ResolutionScopeExtensions
{
    extension(ResolutionScope scope)
    {
        public ResolutionScope<TCandidate, TSearchSpace, TProblem> For<TCandidate, TSearchSpace, TProblem>()
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> => new(scope);

        /// <summary>
        /// Names the search state as well, which an algorithm knows because it is the state the algorithm produces.
        /// The result still serves every role that binds on the triple alone.
        /// </summary>
        public ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> For<TCandidate, TSearchSpace, TProblem, TSearchState>()
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
            where TSearchState : class, ISearchState => new(scope);
    }
}
