using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Interceptors;

public abstract record DecoratorInterceptor<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TState>
  : IInterceptor<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  where TAlgorithmState : class, IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  protected IInterceptor<TGenotype, TSearchSpace, TProblem, TAlgorithmState> InnerInterceptor { get; }
  
  protected DecoratorInterceptor(IInterceptor<TGenotype, TSearchSpace, TProblem, TAlgorithmState> innerInterceptor)
  {
    InnerInterceptor = innerInterceptor;
  }
  
  public IInterceptorInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new Instance(this, instanceRegistry.Resolve(InnerInterceptor), CreateInitialState());
  
  protected abstract TState CreateInitialState();
  
  protected abstract TAlgorithmState Transform(TAlgorithmState currentState, TAlgorithmState? previousState, TState state,
    IInterceptorInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState> innerInterceptor,
    TSearchSpace searchSpace, TProblem problem);
  
  private sealed class Instance(DecoratorInterceptor<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TState> decorator,
    IInterceptorInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState> innerInterceptor, TState state)
    : IInterceptorInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  {
    public TAlgorithmState Transform(TAlgorithmState currentState, TAlgorithmState? previousState, TSearchSpace searchSpace, TProblem problem)
    {
      return decorator.Transform(currentState, previousState, state, innerInterceptor, searchSpace, problem);
    }
  }
}
