using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

public abstract record WrappingTerminator<TGenotype, TSearchState, TSearchSpace, TProblem, TExecutionState>
  : ITerminator<TGenotype, TSearchSpace, TProblem, TSearchState>
  where TSearchState : class, ISearchState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  protected delegate bool InnerShouldTerminate(TSearchState state, TSearchSpace searchSpace, TProblem problem);

  protected ITerminator<TGenotype, TSearchSpace, TProblem, TSearchState> InnerTerminator { get; }

  protected WrappingTerminator(ITerminator<TGenotype, TSearchSpace, TProblem, TSearchState> innerTerminator)
  {
    InnerTerminator = innerTerminator;
  }

  public ITerminatorInstance<TGenotype, TSearchSpace, TProblem, TSearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new Instance(this, instanceRegistry.Resolve(InnerTerminator).ShouldTerminate, CreateInitialState());

  protected abstract TExecutionState CreateInitialState();

  protected abstract bool ShouldTerminate(TSearchState searchState, TExecutionState executionState,
    InnerShouldTerminate innerShouldTerminate,
    TSearchSpace searchSpace, TProblem problem);

  private sealed class Instance(WrappingTerminator<TGenotype, TSearchState, TSearchSpace, TProblem, TExecutionState> wrappingTerminator,
    InnerShouldTerminate innerShouldTerminate, TExecutionState executionState)
    : ITerminatorInstance<TGenotype, TSearchSpace, TProblem, TSearchState>
  {
    public bool ShouldTerminate(TSearchState state, TSearchSpace searchSpace, TProblem problem)
    {
      return wrappingTerminator.ShouldTerminate(state, executionState, innerShouldTerminate, searchSpace, problem);
    }
  }
}

public abstract record WrappingTerminator<TGenotype, TSearchSpace, TProblem, TSearchState>
  : WrappingTerminator<TGenotype, TSearchState, TSearchSpace, TProblem, NoState>
  where TSearchState : class, ISearchState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  protected WrappingTerminator(ITerminator<TGenotype, TSearchSpace, TProblem, TSearchState> innerTerminator)
    : base(innerTerminator)
  {
  }

  protected sealed override NoState CreateInitialState() => NoState.Instance;

  protected sealed override bool ShouldTerminate(TSearchState searchState, NoState executionState,
    InnerShouldTerminate innerShouldTerminate,
    TSearchSpace searchSpace, TProblem problem)
    => ShouldTerminate(searchState, innerShouldTerminate, searchSpace, problem);

  protected abstract bool ShouldTerminate(TSearchState searchState,
    InnerShouldTerminate innerShouldTerminate,
    TSearchSpace searchSpace, TProblem problem);
}
