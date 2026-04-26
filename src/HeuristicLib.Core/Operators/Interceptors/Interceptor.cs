using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Interceptors;

public abstract record Interceptor<TGenotype, TSearchSpace, TProblem, TSearchState, TExecutionState>
  : IInterceptor<TGenotype, TSearchSpace, TProblem, TSearchState>
  where TSearchState : class, ISearchState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TExecutionState : class
{
  protected abstract TExecutionState CreateInitialState();

  protected abstract TSearchState Transform(TSearchState currentState, TSearchState? previousState, TExecutionState executionState,
    TSearchSpace searchSpace, TProblem problem);

  public IInterceptorInstance<TGenotype, TSearchSpace, TProblem, TSearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new InterceptorInstance(this, CreateInitialState());

  private sealed class InterceptorInstance(Interceptor<TGenotype, TSearchSpace, TProblem, TSearchState, TExecutionState> interceptor, TExecutionState executionState)
    : IInterceptorInstance<TGenotype, TSearchSpace, TProblem, TSearchState>
  {
    public TSearchState Transform(TSearchState currentState, TSearchState? previousState, TSearchSpace searchSpace, TProblem problem)
    {
      return interceptor.Transform(currentState, previousState, executionState, searchSpace, problem);
    }
  }
}

public abstract record Interceptor<TGenotype, TSearchSpace, TSearchState, TExecutionState>
  : IInterceptor<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>, TSearchState>
  where TSearchState : class, ISearchState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TExecutionState : class
{
  protected abstract TExecutionState CreateInitialState();

  protected abstract TSearchState Transform(TSearchState currentState, TSearchState? previousState, TExecutionState executionState,
    TSearchSpace searchSpace);

  public IInterceptorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>, TSearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new InterceptorInstance(this, CreateInitialState());

  private sealed class InterceptorInstance(Interceptor<TGenotype, TSearchSpace, TSearchState, TExecutionState> interceptor, TExecutionState executionState)
    : IInterceptorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>, TSearchState>
  {
    public TSearchState Transform(TSearchState currentState, TSearchState? previousState, TSearchSpace searchSpace, IProblem<TGenotype, TSearchSpace> problem)
    {
      return interceptor.Transform(currentState, previousState, executionState, searchSpace);
    }
  }
}

public abstract record Interceptor<TGenotype, TSearchState, TExecutionState>
  : IInterceptor<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>, TSearchState>
  where TSearchState : class, ISearchState
  where TExecutionState : class
{
  protected abstract TExecutionState CreateInitialState();

  protected abstract TSearchState Transform(TSearchState currentState, TSearchState? previousState, TExecutionState executionState);

  public IInterceptorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>, TSearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new InterceptorInstance(this, CreateInitialState());

  private sealed class InterceptorInstance(Interceptor<TGenotype, TSearchState, TExecutionState> interceptor, TExecutionState executionState)
    : IInterceptorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>, TSearchState>
  {
    public TSearchState Transform(TSearchState currentState, TSearchState? previousState, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem)
    {
      return interceptor.Transform(currentState, previousState, executionState);
    }
  }
}



