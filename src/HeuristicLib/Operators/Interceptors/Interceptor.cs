using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Interceptors;

public abstract record Interceptor<TCandidate, TSearchSpace, TProblem, TSearchState, TExecutionState>
  : IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>
  where TSearchState : class, ISearchState
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
  where TExecutionState : class
{
    protected abstract TExecutionState CreateInitialState();

    protected abstract TSearchState Transform(TSearchState currentState, TSearchState? previousState, TExecutionState executionState,
      TSearchSpace searchSpace, TProblem problem);

    public IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
      new InterceptorInstance(this, CreateInitialState());

    private sealed class InterceptorInstance(Interceptor<TCandidate, TSearchSpace, TProblem, TSearchState, TExecutionState> interceptor, TExecutionState executionState)
      : IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>
    {
        public TSearchState Transform(TSearchState currentState, TSearchState? previousState, TSearchSpace searchSpace, TProblem problem)
        {
            return interceptor.Transform(currentState, previousState, executionState, searchSpace, problem);
        }
    }
}

public abstract record Interceptor<TCandidate, TSearchSpace, TSearchState, TExecutionState>
  : IInterceptor<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, TSearchState>
  where TSearchState : class, ISearchState
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TExecutionState : class
{
    protected abstract TExecutionState CreateInitialState();

    protected abstract TSearchState Transform(TSearchState currentState, TSearchState? previousState, TExecutionState executionState,
      TSearchSpace searchSpace);

    public IInterceptorInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, TSearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
      new InterceptorInstance(this, CreateInitialState());

    private sealed class InterceptorInstance(Interceptor<TCandidate, TSearchSpace, TSearchState, TExecutionState> interceptor, TExecutionState executionState)
      : IInterceptorInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, TSearchState>
    {
        public TSearchState Transform(TSearchState currentState, TSearchState? previousState, TSearchSpace searchSpace, IProblem<TCandidate, TSearchSpace> problem)
        {
            return interceptor.Transform(currentState, previousState, executionState, searchSpace);
        }
    }
}

public abstract record Interceptor<TCandidate, TSearchState, TExecutionState>
  : IInterceptor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState>
  where TSearchState : class, ISearchState
  where TExecutionState : class
{
    protected abstract TExecutionState CreateInitialState();

    protected abstract TSearchState Transform(TSearchState currentState, TSearchState? previousState, TExecutionState executionState);

    public IInterceptorInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
      new InterceptorInstance(this, CreateInitialState());

    private sealed class InterceptorInstance(Interceptor<TCandidate, TSearchState, TExecutionState> interceptor, TExecutionState executionState)
      : IInterceptorInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState>
    {
        public TSearchState Transform(TSearchState currentState, TSearchState? previousState, ISearchSpace<TCandidate> searchSpace, IProblem<TCandidate, ISearchSpace<TCandidate>> problem)
        {
            return interceptor.Transform(currentState, previousState, executionState);
        }
    }
}



