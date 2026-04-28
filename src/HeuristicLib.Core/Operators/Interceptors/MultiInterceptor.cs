using Generator.Equals;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Interceptors;

[Equatable]
public abstract partial record MultiInterceptor<TGenotype, TSearchSpace, TProblem, TSearchState, TExecutionState>
  : IInterceptor<TGenotype, TSearchSpace, TProblem, TSearchState>
  where TSearchState : class, ISearchState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  [OrderedEquality] protected ImmutableArray<IInterceptor<TGenotype, TSearchSpace, TProblem, TSearchState>> InnerInterceptors { get; }

  protected delegate TSearchState InnerTransform(TSearchState currentState, TSearchState? previousState, TSearchSpace searchSpace, TProblem problem);

  protected MultiInterceptor(ImmutableArray<IInterceptor<TGenotype, TSearchSpace, TProblem, TSearchState>> innerInterceptors)
  {
    InnerInterceptors = innerInterceptors;
  }

  public IInterceptorInstance<TGenotype, TSearchSpace, TProblem, TSearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new Instance(this, InnerInterceptors.Select(instanceRegistry.Resolve).Select(x => (InnerTransform)x.Transform).ToArray(), CreateInitialState());

  protected abstract TExecutionState CreateInitialState();

  protected abstract TSearchState Transform(TSearchState currentState, TSearchState? previousState, TExecutionState executionState,
    IReadOnlyList<InnerTransform> innerInterceptors,
    TSearchSpace searchSpace, TProblem problem);

  private sealed class Instance(MultiInterceptor<TGenotype, TSearchSpace, TProblem, TSearchState, TExecutionState> multiInterceptor,
    IReadOnlyList<InnerTransform> innerInterceptors, TExecutionState executionState)
    : IInterceptorInstance<TGenotype, TSearchSpace, TProblem, TSearchState>
  {
    public TSearchState Transform(TSearchState currentState, TSearchState? previousState, TSearchSpace searchSpace, TProblem problem)
    {
      return multiInterceptor.Transform(currentState, previousState, executionState, innerInterceptors, searchSpace, problem);
    }
  }
}

public abstract record MultiInterceptor<TGenotype, TSearchSpace, TProblem, TSearchState>
  : MultiInterceptor<TGenotype, TSearchSpace, TProblem, TSearchState, NoState>
  where TSearchState : class, ISearchState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  protected MultiInterceptor(ImmutableArray<IInterceptor<TGenotype, TSearchSpace, TProblem, TSearchState>> innerInterceptors)
    : base(innerInterceptors)
  {
  }

  protected sealed override NoState CreateInitialState() => NoState.Instance;

  protected sealed override TSearchState Transform(TSearchState currentState, TSearchState? previousState,
    NoState executionState, IReadOnlyList<InnerTransform> innerInterceptors,
    TSearchSpace searchSpace, TProblem problem)
    => Transform(currentState, previousState, innerInterceptors, searchSpace, problem);

  protected abstract TSearchState Transform(TSearchState currentState, TSearchState? previousState,
    IReadOnlyList<InnerTransform> innerInterceptors,
    TSearchSpace searchSpace, TProblem problem);
}
