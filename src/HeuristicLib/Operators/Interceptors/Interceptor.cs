using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Interceptors;

/// <remarks>
/// Derive directly from this base when the interceptor owns child execution instances or needs direct control over its execution structure.
/// Use <see cref="StatelessInterceptor{TCandidate,TSearchSpace,TProblem,TSearchState}"/> when no mutable execution data is needed.
/// Use <see cref="StatefulInterceptor{TCandidate,TSearchSpace,TProblem,TSearchState,TState}"/> when only ordinary execution data is needed.
/// </remarks>
public abstract record Interceptor<TCandidate, TSearchSpace, TProblem, TSearchState>
    : IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected abstract IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateInterceptorInstance(ExecutionInstanceRegistry registry);

    IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> IExecutionInstanceResolvable<IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>>.CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
        CreateInterceptorInstance(instanceRegistry);
}

public abstract record Interceptor<TCandidate, TSearchSpace, TSearchState>
    : IInterceptor<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, TSearchState>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    protected abstract IInterceptorInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, TSearchState> CreateInterceptorInstance(ExecutionInstanceRegistry registry);

    IInterceptorInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, TSearchState> IExecutionInstanceResolvable<IInterceptorInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, TSearchState>>.CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
        CreateInterceptorInstance(instanceRegistry);
}

public abstract record Interceptor<TCandidate, TSearchState>
    : IInterceptor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState>
    where TSearchState : class, ISearchState
{
    protected abstract IInterceptorInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState> CreateInterceptorInstance(ExecutionInstanceRegistry registry);

    IInterceptorInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState> IExecutionInstanceResolvable<IInterceptorInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState>>.CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
        CreateInterceptorInstance(instanceRegistry);
}

public abstract class InterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>
    : IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public abstract TSearchState Transform(TSearchState currentState, TSearchState? previousState, TSearchSpace searchSpace, TProblem problem);
}

public abstract class InterceptorInstance<TCandidate, TSearchSpace, TSearchState>
    : IInterceptorInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, TSearchState>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    public abstract TSearchState Transform(TSearchState currentState, TSearchState? previousState, TSearchSpace searchSpace);

    TSearchState IInterceptorInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, TSearchState>.Transform(TSearchState currentState, TSearchState? previousState, TSearchSpace searchSpace, IProblem<TCandidate, TSearchSpace> problem) =>
        Transform(currentState, previousState, searchSpace);
}

public abstract class InterceptorInstance<TCandidate, TSearchState>
    : IInterceptorInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState>
    where TSearchState : class, ISearchState
{
    public abstract TSearchState Transform(TSearchState currentState, TSearchState? previousState);

    TSearchState IInterceptorInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState>.Transform(TSearchState currentState, TSearchState? previousState, ISearchSpace<TCandidate> searchSpace, IProblem<TCandidate, ISearchSpace<TCandidate>> problem) =>
        Transform(currentState, previousState);
}
