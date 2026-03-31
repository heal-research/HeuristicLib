using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Interceptors;

public abstract record StatefulInterceptor<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TState>
  : IInterceptor<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  where TAlgorithmState : class, IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TState : class
{
  protected abstract TState CreateInitialState();

  protected abstract TAlgorithmState Transform(TAlgorithmState currentState, TAlgorithmState? previousState, TState state,
    TSearchSpace searchSpace, TProblem problem);

  public IInterceptorInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new StatefulInterceptorInstance(this, CreateInitialState());

  private sealed class StatefulInterceptorInstance(StatefulInterceptor<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TState> interceptor, TState state)
    : IInterceptorInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  {
    public TAlgorithmState Transform(TAlgorithmState currentState, TAlgorithmState? previousState, TSearchSpace searchSpace, TProblem problem)
    {
      return interceptor.Transform(currentState, previousState, state, searchSpace, problem);
    }
  }
}

public abstract record StatefulInterceptor<TGenotype, TSearchSpace, TAlgorithmState, TState>
  : IInterceptor<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>, TAlgorithmState>
  where TAlgorithmState : class, IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TState : class
{
  protected abstract TState CreateInitialState();

  protected abstract TAlgorithmState Transform(TAlgorithmState currentState, TAlgorithmState? previousState, TState state,
    TSearchSpace searchSpace);

  public IInterceptorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>, TAlgorithmState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new StatefulInterceptorInstance(this, CreateInitialState());

  private sealed class StatefulInterceptorInstance(StatefulInterceptor<TGenotype, TSearchSpace, TAlgorithmState, TState> interceptor, TState state)
    : IInterceptorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>, TAlgorithmState>
  {
    public TAlgorithmState Transform(TAlgorithmState currentState, TAlgorithmState? previousState, TSearchSpace searchSpace, IProblem<TGenotype, TSearchSpace> problem)
    {
      return interceptor.Transform(currentState, previousState, state, searchSpace);
    }
  }
}

public abstract record StatefulInterceptor<TGenotype, TAlgorithmState, TState>
  : IInterceptor<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>, TAlgorithmState>
  where TAlgorithmState : class, IAlgorithmState
  where TState : class
{
  protected abstract TState CreateInitialState();

  protected abstract TAlgorithmState Transform(TAlgorithmState currentState, TAlgorithmState? previousState, TState state);

  public IInterceptorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>, TAlgorithmState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new StatefulInterceptorInstance(this, CreateInitialState());

  private sealed class StatefulInterceptorInstance(StatefulInterceptor<TGenotype, TAlgorithmState, TState> interceptor, TState state)
    : IInterceptorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>, TAlgorithmState>
  {
    public TAlgorithmState Transform(TAlgorithmState currentState, TAlgorithmState? previousState, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem)
    {
      return interceptor.Transform(currentState, previousState, state);
    }
  }
}



