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
    ExecutorFactory<IInterceptionExecutor<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState>> CreateExecutorFactory<TRunSearchSpace, TRunProblem, TRunSearchState>()
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
        where TRunSearchState : class, ISearchState;
}

public interface IInterceptionExecutor<TCandidate, in TSearchSpace, in TProblem, TSearchState>
    : IOperatorExecutor
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    TSearchState Transform(TSearchState currentState, TSearchState? previousState, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

public abstract class InterceptorAdvice<TCandidate> : Advice<IInterceptor<TCandidate>>
{
    /// <summary>Prepares this advice for one join point and returns a constructor accepting the execution it wraps.</summary>
    /// <remarks>
    /// Resolution calls this once per join point and owner state. Validate the run's types and allocate state that must
    /// survive rebuilding the execution graph here.
    /// </remarks>
    /// <param name="interceptor">The configuration the pointcut selected.</param>
    public abstract WrapperExecutorFactory<IInterceptionExecutor<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState>> CreateWrapperFactory<TRunSearchSpace, TRunProblem, TRunSearchState>(IInterceptor<TCandidate> interceptor)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
        where TRunSearchState : class, ISearchState;
}

public static class InterceptorResolutionExtensions
{
    extension(ResolutionScope scope)
    {
        public IInterceptionExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(IInterceptor<TCandidate> interceptor)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
            where TSearchState : class, ISearchState =>
            interceptor is IOperator<Interception, TCandidate> prepared
                ? scope.ResolveOperator<Interception, TCandidate, TSearchSpace, TProblem, TSearchState, IInterceptionExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>>(
                    prepared, Interception.CreateExecutorAdapter<TCandidate, TSearchSpace, TProblem, TSearchState>()).Executor
                : scope.Resolve(interceptor, static source => source.CreateExecutorFactory<TSearchSpace, TProblem, TSearchState>(), CreateWrapperFactory<TCandidate, TSearchSpace, TProblem, TSearchState>);

        [return: NotNullIfNotNull(nameof(interceptor))]
        public IInterceptionExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>? ResolveOptional<TCandidate, TSearchSpace, TProblem, TSearchState>(IInterceptor<TCandidate>? interceptor)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
            where TSearchState : class, ISearchState =>
            interceptor is null ? null : scope.Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(interceptor);

        public bool TryResolve<TCandidate, TSearchSpace, TProblem, TSearchState>(
            IInterceptor<TCandidate> interceptor,
            [NotNullWhen(true)] out IInterceptionExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>? executor,
            [NotNullWhen(false)] out string? reason)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
            where TSearchState : class, ISearchState
        {
            try
            {
                executor = scope.Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(interceptor);
                reason = null;
                return true;
            }
            catch (InvalidOperationException exception)
            {
                executor = null;
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
        public IInterceptionExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> Resolve(IInterceptor<TCandidate> interceptor) =>
            scope.Scope.Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(interceptor);

        [return: NotNullIfNotNull(nameof(interceptor))]
        public IInterceptionExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>? ResolveOptional(IInterceptor<TCandidate>? interceptor) =>
            interceptor is null ? null : scope.Scope.Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(interceptor);

        public bool TryResolve(
            IInterceptor<TCandidate> interceptor,
            [NotNullWhen(true)] out IInterceptionExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>? executor,
            [NotNullWhen(false)] out string? reason) =>
            scope.Scope.TryResolve(interceptor, out executor, out reason);
    }

    private static WrapperExecutorFactory<IInterceptionExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>> CreateWrapperFactory<TCandidate, TSearchSpace, TProblem, TSearchState>(Advice advice, IInterceptor<TCandidate> interceptor)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState =>
        ((InterceptorAdvice<TCandidate>)advice).CreateWrapperFactory<TSearchSpace, TProblem, TSearchState>(interceptor);
}
