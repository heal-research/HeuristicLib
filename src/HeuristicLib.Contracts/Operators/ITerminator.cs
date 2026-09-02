using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>
/// A terminator configuration. It names only the candidate representation; the search space, problem and search state
/// types are supplied when an execution instance is created.
/// </summary>
/// <remarks>
/// The search state is supplied rather than named because a terminator does not originate one: the algorithm it runs
/// in does, and that algorithm is what resolves the terminator. So there is a source to bind against at resolution
/// time, exactly as there is for the search space and problem, and a terminator written for a state the algorithm does
/// not produce is reported when the execution graph is built.
/// </remarks>
public interface ITerminator<TCandidate> : IOperator
{
    /// <summary>
    /// Creates an execution instance for the search space, problem and search state a run supplies.
    /// </summary>
    ITerminatorInstance<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> CreateExecutionInstance<TRunSearchSpace, TRunProblem, TRunSearchState>(ExecutionInstanceRegistry instanceRegistry)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
        where TRunSearchState : class, ISearchState;
}

public interface ITerminatorInstance<TCandidate, in TSearchSpace, in TProblem, in TSearchState>
  : IOperatorInstance
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
  where TSearchState : class, ISearchState
{
    bool IsTerminalState(TSearchState state, TSearchSpace searchSpace, TProblem problem);
}

/// <summary>
/// Resolution for the terminator role, declared next to the role because the registry cannot name a role generically.
/// </summary>
/// <remarks>
/// Resolve from the registry, which is what a creation method is handed. The resolver form for this role takes the
/// <em>quadruple</em> — <see cref="ExecutionInstanceResolver{TCandidate, TSearchSpace, TProblem, TSearchState}"/>,
/// built with <c>registry.For&lt;TCandidate, TSearchSpace, TProblem, TSearchState&gt;()</c> — because the search
/// state has to come from somewhere and a method type argument cannot express it. That resolver derives from the
/// triple one, so one object still serves every role an algorithm resolves.
/// </remarks>
public static class TerminatorResolverExtensions
{
    extension(ExecutionInstanceRegistry registry)
    {
        /// <summary>Resolves a terminator over a triple named at this call site.</summary>
        public ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(ITerminator<TCandidate> terminator)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
            where TSearchState : class, ISearchState =>
            registry.Resolve(terminator, static (creationTarget, childRegistry) => creationTarget.CreateExecutionInstance<TSearchSpace, TProblem, TSearchState>(childRegistry));

        /// <summary>Resolves a terminator that may be absent, returning <see langword="null"/> when it is.</summary>
        public ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>? ResolveOptional<TCandidate, TSearchSpace, TProblem, TSearchState>(ITerminator<TCandidate>? terminator)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
            where TSearchState : class, ISearchState =>
            terminator is null ? null : registry.Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(terminator);

        /// <summary>
        /// Resolves a terminator, reporting rather than throwing when it cannot run over this search space and problem.
        /// </summary>
        /// <remarks>See <see cref="CrossoverResolverExtensions"/> for why this is the binding check.</remarks>
        public bool TryResolve<TCandidate, TSearchSpace, TProblem, TSearchState>(
            ITerminator<TCandidate> terminator,
            [NotNullWhen(true)] out ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>? instance,
            [NotNullWhen(false)] out string? reason)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
            where TSearchState : class, ISearchState
        {
            try
            {
                instance = registry.Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(terminator);
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
        /// <summary>Resolves a terminator over the search space, problem and state this resolver carries.</summary>
        /// <remarks>
        /// Nothing is named at the call site: all four come from the resolver, which is why this role's resolver form
        /// takes the quadruple rather than the triple the other seven use.
        /// </remarks>
        public ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> Resolve(ITerminator<TCandidate> terminator) =>
            resolver.Registry.Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(terminator);

        /// <summary>Resolves a terminator that may be absent, returning <see langword="null"/> when it is.</summary>
        public ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>? ResolveOptional(ITerminator<TCandidate>? terminator) =>
            terminator is null ? null : resolver.Registry.Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(terminator);

        /// <summary>
        /// Resolves a terminator, reporting rather than throwing when it cannot run over this resolver's search space,
        /// problem and search state.
        /// </summary>
        public bool TryResolve(
            ITerminator<TCandidate> terminator,
            [NotNullWhen(true)] out ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>? instance,
            [NotNullWhen(false)] out string? reason) =>
            resolver.Registry.TryResolve<TCandidate, TSearchSpace, TProblem, TSearchState>(terminator, out instance, out reason);
    }
}
