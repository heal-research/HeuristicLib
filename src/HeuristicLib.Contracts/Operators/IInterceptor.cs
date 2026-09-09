using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <remarks>
/// An interceptor written for a search space, problem or state the run does not supply is reported when the execution
/// graph is built. The state must match exactly, since <c>Transform</c> returns it as well as reading it.
/// </remarks>
public interface IInterceptor<TCandidate> : IOperator
{
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

public static class InterceptorResolverExtensions
{
    extension(ExecutionInstanceRegistry registry)
    {
        public IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(IInterceptor<TCandidate> interceptor)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
            where TSearchState : class, ISearchState =>
            registry.Resolve(interceptor, static (creationTarget, childRegistry) => creationTarget.CreateExecutionInstance<TSearchSpace, TProblem, TSearchState>(childRegistry));

        public IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>? ResolveOptional<TCandidate, TSearchSpace, TProblem, TSearchState>(IInterceptor<TCandidate>? interceptor)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
            where TSearchState : class, ISearchState =>
            interceptor is null ? null : registry.Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(interceptor);

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
        public IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> Resolve(IInterceptor<TCandidate> interceptor) =>
            resolver.Registry.Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(interceptor);

        public IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>? ResolveOptional(IInterceptor<TCandidate>? interceptor) =>
            interceptor is null ? null : resolver.Registry.Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(interceptor);

        public bool TryResolve(
            IInterceptor<TCandidate> interceptor,
            [NotNullWhen(true)] out IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>? instance,
            [NotNullWhen(false)] out string? reason) =>
            resolver.Registry.TryResolve(interceptor, out instance, out reason);
    }
}
