using Generator.Equals;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Interceptors;

[Equatable]
public abstract partial record MultiInterceptor<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TState>
  : IInterceptor<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  where TAlgorithmState : class, IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  [OrderedEquality] protected ImmutableArray<IInterceptor<TGenotype, TSearchSpace, TProblem, TAlgorithmState>> InnerInterceptors { get; }

  protected delegate TAlgorithmState InnerTransform(TAlgorithmState currentState, TAlgorithmState? previousState, TSearchSpace searchSpace, TProblem problem);

  protected MultiInterceptor(ImmutableArray<IInterceptor<TGenotype, TSearchSpace, TProblem, TAlgorithmState>> innerInterceptors)
  {
    InnerInterceptors = innerInterceptors;
  }

  public IInterceptorInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new Instance(this, InnerInterceptors.Select(instanceRegistry.Resolve).Select(x => (InnerTransform)x.Transform).ToArray(), CreateInitialState());

  protected abstract TState CreateInitialState();

  protected abstract TAlgorithmState Transform(TAlgorithmState currentState, TAlgorithmState? previousState, TState state,
    IReadOnlyList<InnerTransform> innerInterceptors,
    TSearchSpace searchSpace, TProblem problem);

  private sealed class Instance(MultiInterceptor<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TState> multiInterceptor,
    IReadOnlyList<InnerTransform> innerInterceptors, TState state)
    : IInterceptorInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  {
    public TAlgorithmState Transform(TAlgorithmState currentState, TAlgorithmState? previousState, TSearchSpace searchSpace, TProblem problem)
    {
      return multiInterceptor.Transform(currentState, previousState, state, innerInterceptors, searchSpace, problem);
    }
  }
}

public abstract record MultiInterceptor<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  : MultiInterceptor<TGenotype, TSearchSpace, TProblem, TAlgorithmState, NoState>
  where TAlgorithmState : class, IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  protected MultiInterceptor(ImmutableArray<IInterceptor<TGenotype, TSearchSpace, TProblem, TAlgorithmState>> innerInterceptors)
    : base(innerInterceptors)
  {
  }

  protected sealed override NoState CreateInitialState() => NoState.Instance;

  protected sealed override TAlgorithmState Transform(TAlgorithmState currentState, TAlgorithmState? previousState,
    NoState state, IReadOnlyList<InnerTransform> innerInterceptors,
    TSearchSpace searchSpace, TProblem problem)
    => Transform(currentState, previousState, innerInterceptors, searchSpace, problem);

  protected abstract TAlgorithmState Transform(TAlgorithmState currentState, TAlgorithmState? previousState,
    IReadOnlyList<InnerTransform> innerInterceptors,
    TSearchSpace searchSpace, TProblem problem);
}
