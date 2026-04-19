using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

public abstract record WrappingTerminator<TGenotype, TAlgorithmState, TSearchSpace, TProblem, TState>
  : ITerminator<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  where TAlgorithmState : class, IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  protected delegate bool InnerShouldTerminate(TAlgorithmState state, TSearchSpace searchSpace, TProblem problem);

  protected ITerminator<TGenotype, TSearchSpace, TProblem, TAlgorithmState> InnerTerminator { get; }

  protected WrappingTerminator(ITerminator<TGenotype, TSearchSpace, TProblem, TAlgorithmState> innerTerminator)
  {
    InnerTerminator = innerTerminator;
  }

  public ITerminatorInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new Instance(this, instanceRegistry.Resolve(InnerTerminator).ShouldTerminate, CreateInitialState());

  protected abstract TState CreateInitialState();

  protected abstract bool ShouldTerminate(TAlgorithmState algorithmState, TState state,
    InnerShouldTerminate innerShouldTerminate,
    TSearchSpace searchSpace, TProblem problem);

  private sealed class Instance(WrappingTerminator<TGenotype, TAlgorithmState, TSearchSpace, TProblem, TState> wrappingTerminator,
    InnerShouldTerminate innerShouldTerminate, TState terminatorState)
    : ITerminatorInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  {
    public bool ShouldTerminate(TAlgorithmState state, TSearchSpace searchSpace, TProblem problem)
    {
      return wrappingTerminator.ShouldTerminate(state, terminatorState, innerShouldTerminate, searchSpace, problem);
    }
  }
}

public abstract record WrappingTerminator<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  : WrappingTerminator<TGenotype, TAlgorithmState, TSearchSpace, TProblem, NoState>
  where TAlgorithmState : class, IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  protected WrappingTerminator(ITerminator<TGenotype, TSearchSpace, TProblem, TAlgorithmState> innerTerminator)
    : base(innerTerminator)
  {
  }

  protected sealed override NoState CreateInitialState() => NoState.Instance;

  protected sealed override bool ShouldTerminate(TAlgorithmState algorithmState, NoState state,
    InnerShouldTerminate innerShouldTerminate,
    TSearchSpace searchSpace, TProblem problem)
    => ShouldTerminate(algorithmState, innerShouldTerminate, searchSpace, problem);

  protected abstract bool ShouldTerminate(TAlgorithmState algorithmState,
    InnerShouldTerminate innerShouldTerminate,
    TSearchSpace searchSpace, TProblem problem);
}
