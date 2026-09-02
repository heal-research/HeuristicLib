using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>
/// An interceptor configuration. It names only the candidate representation; the search space, problem and search
/// state types are supplied when an execution instance is created.
/// </summary>
/// <remarks>
/// The search state is supplied rather than named because an interceptor does not originate one: it transforms the
/// state the algorithm hands it, and that algorithm is what resolves the interceptor. An interceptor written for a
/// state the algorithm does not produce is reported when the execution graph is built. The match must be exact, since
/// <see cref="IInterceptorInstance{TCandidate, TSearchSpace, TProblem, TSearchState}.Transform"/> returns the state as
/// well as reading it.
/// </remarks>
public interface IInterceptor<TCandidate> : IOperator
{
    /// <summary>
    /// Creates an execution instance for the search space, problem and search state a run supplies.
    /// </summary>
    IInterceptorInstance<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> CreateExecutionInstance<TRunSearchSpace, TRunProblem, TRunSearchState>(ExecutionInstanceRegistry instanceRegistry)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
        where TRunSearchState : class, ISearchState;
}

public interface IInterceptorInstance<TCandidate, in TSearchSpace, in TProblem, TSearchState>
  : IOperatorInstance
  where TSearchState : class, ISearchState
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    TSearchState Transform(TSearchState currentState, TSearchState? previousState, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

/// <summary>
/// Resolution for the interceptor role, declared next to the role because the registry cannot name a role generically.
/// </summary>
/// <remarks>
/// Resolve from the registry, which is what a creation method is handed. The resolver form for this role takes the
/// <em>quadruple</em> — <see cref="ExecutionInstanceResolver{TCandidate, TSearchSpace, TProblem, TSearchState}"/>,
/// built with <c>registry.For&lt;TCandidate, TSearchSpace, TProblem, TSearchState&gt;()</c> — because the search
/// state has to come from somewhere and a method type argument cannot express it. That resolver derives from the
/// triple one, so one object still serves every role an algorithm resolves.
/// </remarks>
public static class InterceptorResolverExtensions
{
    extension(ExecutionInstanceRegistry registry)
    {
        /// <summary>Resolves an interceptor over a triple named at this call site.</summary>
        public IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(IInterceptor<TCandidate> interceptor)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
            where TSearchState : class, ISearchState =>
            registry.Resolve(interceptor, static (creationTarget, childRegistry) => creationTarget.CreateExecutionInstance<TSearchSpace, TProblem, TSearchState>(childRegistry));

        /// <summary>Resolves an interceptor that may be absent, returning <see langword="null"/> when it is.</summary>
        public IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>? ResolveOptional<TCandidate, TSearchSpace, TProblem, TSearchState>(IInterceptor<TCandidate>? interceptor)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
            where TSearchState : class, ISearchState =>
            interceptor is null ? null : registry.Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(interceptor);

        /// <summary>
        /// Resolves an interceptor, reporting rather than throwing when it cannot run over this search space and problem.
        /// </summary>
        /// <remarks>See <see cref="CrossoverResolverExtensions"/> for why this is the binding check.</remarks>
        public bool TryResolve<TCandidate, TSearchSpace, TProblem, TSearchState>(
            IInterceptor<TCandidate> interceptor,
            [NotNullWhen(true)] out IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>? instance,
            [NotNullWhen(false)] out string? reason)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
            where TSearchState : class, ISearchState
        {
            try
            {
                instance = registry.Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(interceptor);
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
        /// <summary>Resolves an interceptor over the search space, problem and state this resolver carries.</summary>
        /// <remarks>
        /// Nothing is named at the call site: all four come from the resolver, which is why this role's resolver form
        /// takes the quadruple rather than the triple the other seven use.
        /// </remarks>
        public IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> Resolve(IInterceptor<TCandidate> interceptor) =>
            resolver.Registry.Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(interceptor);

        /// <summary>Resolves an interceptor that may be absent, returning <see langword="null"/> when it is.</summary>
        public IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>? ResolveOptional(IInterceptor<TCandidate>? interceptor) =>
            interceptor is null ? null : resolver.Registry.Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(interceptor);

        /// <summary>
        /// Resolves an interceptor, reporting rather than throwing when it cannot run over this resolver's search space,
        /// problem and search state.
        /// </summary>
        public bool TryResolve(
            IInterceptor<TCandidate> interceptor,
            [NotNullWhen(true)] out IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>? instance,
            [NotNullWhen(false)] out string? reason) =>
            resolver.Registry.TryResolve<TCandidate, TSearchSpace, TProblem, TSearchState>(interceptor, out instance, out reason);
    }
}
