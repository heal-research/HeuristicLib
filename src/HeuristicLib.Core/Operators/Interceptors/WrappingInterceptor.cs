using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Interceptors;

public abstract record WrappingInterceptor<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TState>
  : IInterceptor<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  where TAlgorithmState : class, IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  protected delegate TAlgorithmState InnerTransform(TAlgorithmState currentState, TAlgorithmState? previousState, TSearchSpace searchSpace, TProblem problem);

  protected IInterceptor<TGenotype, TSearchSpace, TProblem, TAlgorithmState> InnerInterceptor { get; }

  protected WrappingInterceptor(IInterceptor<TGenotype, TSearchSpace, TProblem, TAlgorithmState> innerInterceptor)
  {
    InnerInterceptor = innerInterceptor;
  }

  public IInterceptorInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new Instance(this, instanceRegistry.Resolve(InnerInterceptor).Transform, CreateInitialState());

  protected abstract TState CreateInitialState();

  protected abstract TAlgorithmState Transform(TAlgorithmState currentState, TAlgorithmState? previousState, TState state,
    InnerTransform innerTransform,
    TSearchSpace searchSpace, TProblem problem);

  private sealed class Instance(WrappingInterceptor<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TState> wrappingInterceptor,
    InnerTransform innerTransform, TState state)
    : IInterceptorInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  {
    public TAlgorithmState Transform(TAlgorithmState currentState, TAlgorithmState? previousState, TSearchSpace searchSpace, TProblem problem)
    {
      return wrappingInterceptor.Transform(currentState, previousState, state, innerTransform, searchSpace, problem);
    }
  }
}

public abstract record WrappingInterceptor<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  : WrappingInterceptor<TGenotype, TSearchSpace, TProblem, TAlgorithmState, NoState>
  where TAlgorithmState : class, IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  protected WrappingInterceptor(IInterceptor<TGenotype, TSearchSpace, TProblem, TAlgorithmState> innerInterceptor)
    : base(innerInterceptor)
  {
  }

  protected sealed override NoState CreateInitialState() => NoState.Instance;

  protected sealed override TAlgorithmState Transform(TAlgorithmState currentState, TAlgorithmState? previousState,
    NoState state, InnerTransform innerTransform,
    TSearchSpace searchSpace, TProblem problem)
    => Transform(currentState, previousState, innerTransform, searchSpace, problem);

  protected abstract TAlgorithmState Transform(TAlgorithmState currentState, TAlgorithmState? previousState,
    InnerTransform innerTransform,
    TSearchSpace searchSpace, TProblem problem);
}
