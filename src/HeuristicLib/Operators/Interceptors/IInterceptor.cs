using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators.Interceptors;
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
    ExecutionFactory<IInterceptorExecution<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState>> CreateExecutionFactory<TRunSearchSpace, TRunProblem, TRunSearchState>()
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
            scope.Resolve(interceptor, static source => source.CreateExecutionFactory<TSearchSpace, TProblem, TSearchState>(), PrepareAdvice<TCandidate, TSearchSpace, TProblem, TSearchState>);

        [return: NotNullIfNotNull(nameof(interceptor))]
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

        [return: NotNullIfNotNull(nameof(interceptor))]
        public IInterceptorExecution<TCandidate, TSearchSpace, TProblem, TSearchState>? ResolveOptional(IInterceptor<TCandidate>? interceptor) =>
            interceptor is null ? null : scope.Scope.Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(interceptor);

        public bool TryResolve(
            IInterceptor<TCandidate> interceptor,
            [NotNullWhen(true)] out IInterceptorExecution<TCandidate, TSearchSpace, TProblem, TSearchState>? execution,
            [NotNullWhen(false)] out string? reason) =>
            scope.Scope.TryResolve(interceptor, out execution, out reason);
    }

    private static Advice<IInterceptorExecution<TCandidate, TSearchSpace, TProblem, TSearchState>> PrepareAdvice<TCandidate, TSearchSpace, TProblem, TSearchState>(IConfigurationNode wrapper)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        if (wrapper is not WrappingInterceptor<TCandidate> typed)
            throw new InvalidOperationException($"{wrapper.GetType().Name} is not a single-child wrapper for {typeof(IInterceptor<TCandidate>).Name} producing {typeof(IInterceptorExecution<TCandidate, TSearchSpace, TProblem, TSearchState>).Name}.");
        return WrappingInterceptor<TCandidate>.CreateWrapperFactory<TSearchSpace, TProblem, TSearchState>(typed).Invoke;
    }
}
