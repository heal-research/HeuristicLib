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
    protected ValueArray<IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>> InnerInterceptors { get; }

    protected MultiInterceptor(IReadOnlyList<IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>> innerInterceptors)
    {
        InnerInterceptors = innerInterceptors.ToValueArray();
    }

    protected sealed override IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateInterceptorInstance(ExecutionInstanceRegistry registry) =>
        CreateInterceptorInstance([.. InnerInterceptors.Select(registry.Resolve)]);

    protected abstract MultiInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateInterceptorInstance(ImmutableArray<IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>> innerInterceptors);
}

public abstract class MultiInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>(ImmutableArray<IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>> innerInterceptors)
    : InterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected ImmutableArray<IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>> InnerInterceptors { get; } = innerInterceptors;
}
