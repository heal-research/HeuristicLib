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
    IInterceptorExecution<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> CreateExecutionInstance<TRunSearchSpace, TRunProblem, TRunSearchState>(ResolutionScope scope)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
        where TRunSearchState : class, ISearchState;
}

public interface IInterceptorExecution<TCandidate, in TSearchSpace, in TProblem, TSearchState>
    : IOperatorExecution
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    TSearchState Transform(TSearchState currentState, TSearchState? previousState, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

public static class InterceptorResolutionExtensions
{
    extension(ResolutionScope scope)
    {
        public IInterceptorExecution<TCandidate, TSearchSpace, TProblem, TSearchState> Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(IInterceptor<TCandidate> interceptor)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
            where TSearchState : class, ISearchState =>
            scope.Resolve(interceptor, static (creationTarget, childScope) => creationTarget.CreateExecutionInstance<TSearchSpace, TProblem, TSearchState>(childScope));

        public IInterceptorExecution<TCandidate, TSearchSpace, TProblem, TSearchState>? ResolveOptional<TCandidate, TSearchSpace, TProblem, TSearchState>(IInterceptor<TCandidate>? interceptor)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
            where TSearchState : class, ISearchState =>
            interceptor is null ? null : scope.Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(interceptor);

        public bool TryResolve<TCandidate, TSearchSpace, TProblem, TSearchState>(
            IInterceptor<TCandidate> interceptor,
            [NotNullWhen(true)] out IInterceptorExecution<TCandidate, TSearchSpace, TProblem, TSearchState>? execution,
            [NotNullWhen(false)] out string? reason)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
            where TSearchState : class, ISearchState
        {
            try
            {
                execution = scope.Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(interceptor);
                reason = null;
                return true;
            }
            catch (InvalidOperationException exception)
            {
                execution = null;
                reason = exception.Message;
                return false;
            }
        }
    }

    extension<TCandidate, TSearchSpace, TProblem, TSearchState>(ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        public IInterceptorExecution<TCandidate, TSearchSpace, TProblem, TSearchState> Resolve(IInterceptor<TCandidate> interceptor) =>
            scope.Scope.Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(interceptor);

        public IInterceptorExecution<TCandidate, TSearchSpace, TProblem, TSearchState>? ResolveOptional(IInterceptor<TCandidate>? interceptor) =>
            interceptor is null ? null : scope.Scope.Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(interceptor);

        public bool TryResolve(
            IInterceptor<TCandidate> interceptor,
            [NotNullWhen(true)] out IInterceptorExecution<TCandidate, TSearchSpace, TProblem, TSearchState>? execution,
            [NotNullWhen(false)] out string? reason) =>
            scope.Scope.TryResolve(interceptor, out execution, out reason);
    }
}
