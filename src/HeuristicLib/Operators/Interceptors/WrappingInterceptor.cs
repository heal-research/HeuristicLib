using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Interceptors;

public abstract record WrappingInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>
    : Interceptor<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> InnerInterceptor { get; }

    protected WrappingInterceptor(IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> innerInterceptor)
    {
        InnerInterceptor = innerInterceptor;
    }

    protected sealed override IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateInterceptorInstance(ExecutionInstanceRegistry registry) =>
        CreateInterceptorInstance(registry.Resolve(InnerInterceptor));

    protected abstract WrappingInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateInterceptorInstance(IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> innerInterceptor);
}

public abstract class WrappingInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>(IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> innerInterceptor)
    : InterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> InnerInterceptor { get; } = innerInterceptor;
}
