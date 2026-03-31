using Generator.Equals;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Interceptors;

[Equatable]
public abstract partial record CompositeInterceptor<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TState>
  : IInterceptor<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  where TAlgorithmState : class, IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  [OrderedEquality] protected ImmutableArray<IInterceptor<TGenotype, TSearchSpace, TProblem, TAlgorithmState>> InnerInterceptors { get; }
  
  protected CompositeInterceptor(ImmutableArray<IInterceptor<TGenotype, TSearchSpace, TProblem, TAlgorithmState>> innerInterceptors)
  {
    InnerInterceptors = innerInterceptors;
  }
  
  public IInterceptorInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new Instance(this, InnerInterceptors.Select(instanceRegistry.Resolve).ToArray(), CreateInitialState());
  
  protected abstract TState CreateInitialState();
  
  protected abstract TAlgorithmState Transform(TAlgorithmState currentState, TAlgorithmState? previousState, TState state,
    IReadOnlyList<IInterceptorInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState>> innerInterceptors,
    TSearchSpace searchSpace, TProblem problem);
  
  private sealed class Instance(CompositeInterceptor<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TState> composite,
    IReadOnlyList<IInterceptorInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState>> innerInterceptors, TState state)
    : IInterceptorInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  {
    public TAlgorithmState Transform(TAlgorithmState currentState, TAlgorithmState? previousState, TSearchSpace searchSpace, TProblem problem)
    {
      return composite.Transform(currentState, previousState, state, innerInterceptors, searchSpace, problem);
    }
  }
}
