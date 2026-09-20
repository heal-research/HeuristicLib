using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>
/// A resolver builder that names the search space and problem once, for an analyzer whose observations all read the
/// same types.
/// </summary>
/// <remarks>
/// Convenience only: every overload forwards to the builder overload of the same name. It is what lets a method group
/// typed at concrete types be passed without naming those types at every call.
/// </remarks>
public class TypedObservationBuilder<TCandidate, TSearchSpace, TProblem>(ExecutionInstanceResolverBuilder builder)
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public ExecutionInstanceResolverBuilder Builder => builder;
}

/// <summary>
/// A resolver builder that also names the search state, for an analyzer observing interceptors.
/// </summary>
/// <remarks>
/// Interceptors name only their candidate, so the state they transform comes from the builder rather than from the call
/// site.
/// </remarks>
public sealed class TypedObservationBuilder<TCandidate, TSearchSpace, TProblem, TSearchState>(ExecutionInstanceResolverBuilder builder)
    : TypedObservationBuilder<TCandidate, TSearchSpace, TProblem>(builder)
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState;

/// <summary>Creates a typed observation builder, naming the types once per installation.</summary>
public static class TypedObservationBuilderExtensions
{
    extension(ExecutionInstanceResolverBuilder builder)
    {
        public TypedObservationBuilder<TCandidate, TSearchSpace, TProblem> For<TCandidate, TSearchSpace, TProblem>()
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> => new(builder);

        /// <summary>
        /// Names the search state as well, which interceptor observations need. The result still observes every role
        /// that binds on the triple alone.
        /// </summary>
        public TypedObservationBuilder<TCandidate, TSearchSpace, TProblem, TSearchState> For<TCandidate, TSearchSpace, TProblem, TSearchState>()
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
            where TSearchState : class, ISearchState => new(builder);
    }
}
