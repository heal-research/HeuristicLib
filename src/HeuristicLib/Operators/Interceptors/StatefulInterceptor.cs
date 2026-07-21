using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Interceptors;

/// <remarks>
/// <typeparamref name="TState"/> may contain mutable execution data and helper data structures.
/// It must not contain operator or algorithm configurations, execution instances or execution instance resolution facilities.
/// <see cref="CreateInitialState"/> must return a fresh state object for every execution instance. Calls are not inherently thread safe.
/// </remarks>
public abstract record StatefulInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState, TState>
    : Interceptor<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TState : class
{
    protected abstract TState CreateInitialState();

    protected abstract TSearchState Transform(TSearchState currentState, TSearchState? previousState, TState state, TSearchSpace searchSpace, TProblem problem);

    protected sealed override IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateInterceptorInstance(ExecutionInstanceRegistry registry) => new Instance(this, CreateInitialState());

    private sealed class Instance(StatefulInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState, TState> interceptor, TState state) : IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>
    {
        public TSearchState Transform(TSearchState currentState, TSearchState? previousState, TSearchSpace searchSpace, TProblem problem) => interceptor.Transform(currentState, previousState, state, searchSpace, problem);
    }
}

public abstract record StatefulInterceptor<TCandidate, TSearchSpace, TSearchState, TState>
    : Interceptor<TCandidate, TSearchSpace, TSearchState>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TState : class
{
    protected abstract TState CreateInitialState();

    protected abstract TSearchState Transform(TSearchState currentState, TSearchState? previousState, TState state, TSearchSpace searchSpace);

    protected sealed override IInterceptorInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, TSearchState> CreateInterceptorInstance(ExecutionInstanceRegistry registry) => new Instance(this, CreateInitialState());

    private sealed class Instance(StatefulInterceptor<TCandidate, TSearchSpace, TSearchState, TState> interceptor, TState state) : IInterceptorInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, TSearchState>
    {
        public TSearchState Transform(TSearchState currentState, TSearchState? previousState, TSearchSpace searchSpace, IProblem<TCandidate, TSearchSpace> problem) => interceptor.Transform(currentState, previousState, state, searchSpace);
    }
}

public abstract record StatefulInterceptor<TCandidate, TSearchState, TState>
    : Interceptor<TCandidate, TSearchState>
    where TSearchState : class, ISearchState
    where TState : class
{
    protected abstract TState CreateInitialState();

    protected abstract TSearchState Transform(TSearchState currentState, TSearchState? previousState, TState state);

    protected sealed override IInterceptorInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState> CreateInterceptorInstance(ExecutionInstanceRegistry registry) => new Instance(this, CreateInitialState());

    private sealed class Instance(StatefulInterceptor<TCandidate, TSearchState, TState> interceptor, TState state) : IInterceptorInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState>
    {
        public TSearchState Transform(TSearchState currentState, TSearchState? previousState, ISearchSpace<TCandidate> searchSpace, IProblem<TCandidate, ISearchSpace<TCandidate>> problem) => interceptor.Transform(currentState, previousState, state);
    }
}
