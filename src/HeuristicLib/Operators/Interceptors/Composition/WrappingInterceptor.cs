using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Interceptors;

public abstract record WrappingInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>
    : Interceptor<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected WrappingInterceptor(IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> childInterceptor)
    {
        ChildInterceptor = childInterceptor;
    }

    public IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> ChildInterceptor { get; init; }

    public sealed override IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
        CreateExecutionInstance(instanceRegistry.Resolve(ChildInterceptor));

    protected abstract WrappingInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutionInstance(IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> childInterceptor);
}

public abstract class WrappingInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>(IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> childInterceptor)
    : InterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> ChildInterceptor { get; } = childInterceptor;
}
