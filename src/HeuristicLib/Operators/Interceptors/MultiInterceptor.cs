using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Interceptors;

public abstract record MultiInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>
    : Interceptor<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected MultiInterceptor(IReadOnlyList<IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>> childInterceptors)
    {
        ChildInterceptors = childInterceptors.ToValueArray();
    }

    public ValueArray<IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>> ChildInterceptors { get; init; }

    public sealed override IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
        CreateExecutionInstance([.. ChildInterceptors.Select(instanceRegistry.Resolve)]);

    protected abstract MultiInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutionInstance(ImmutableArray<IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>> childInterceptors);
}

public abstract class MultiInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>(ImmutableArray<IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>> childInterceptors)
    : InterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected ImmutableArray<IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>> ChildInterceptors { get; } = childInterceptors;
}
