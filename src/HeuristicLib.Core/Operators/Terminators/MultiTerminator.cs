using Generator.Equals;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

[Equatable]
public abstract partial record MultiTerminator<TGenotype, TAlgorithmState, TSearchSpace, TProblem, TState>
  : ITerminator<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  where TAlgorithmState : class, IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  [OrderedEquality] protected ImmutableArray<ITerminator<TGenotype, TSearchSpace, TProblem, TAlgorithmState>> InnerTerminators { get; }

  protected delegate bool InnerShouldTerminate(TAlgorithmState algorithmState, TSearchSpace searchSpace, TProblem problem);

  protected MultiTerminator(ImmutableArray<ITerminator<TGenotype, TSearchSpace, TProblem, TAlgorithmState>> innerTerminators)
  {
    InnerTerminators = innerTerminators;
  }

  public ITerminatorInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new Instance(this, InnerTerminators.Select(instanceRegistry.Resolve).Select(x => (InnerShouldTerminate)x.ShouldTerminate).ToArray(), CreateInitialState());

  protected abstract TState CreateInitialState();

  protected abstract bool ShouldTerminate(TAlgorithmState algorithmState, TState state,
    IReadOnlyList<InnerShouldTerminate> innerTerminators,
    TSearchSpace searchSpace, TProblem problem);

  private sealed class Instance(MultiTerminator<TGenotype, TAlgorithmState, TSearchSpace, TProblem, TState> multiTerminator,
    IReadOnlyList<InnerShouldTerminate> innerTerminators,
    TState terminatorState)
    : ITerminatorInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  {
    public bool ShouldTerminate(TAlgorithmState state, TSearchSpace searchSpace, TProblem problem)
    {
      return multiTerminator.ShouldTerminate(state, terminatorState, innerTerminators, searchSpace, problem);
    }
  }
}

public abstract record MultiTerminator<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  : MultiTerminator<TGenotype, TAlgorithmState, TSearchSpace, TProblem, NoState>
  where TAlgorithmState : class, IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  protected MultiTerminator(ImmutableArray<ITerminator<TGenotype, TSearchSpace, TProblem, TAlgorithmState>> innerTerminators)
    : base(innerTerminators)
  {
  }

  protected sealed override NoState CreateInitialState() => NoState.Instance;

  protected sealed override bool ShouldTerminate(TAlgorithmState algorithmState, NoState state,
    IReadOnlyList<InnerShouldTerminate> innerTerminators,
    TSearchSpace searchSpace, TProblem problem)
    => ShouldTerminate(algorithmState, innerTerminators, searchSpace, problem);

  protected abstract bool ShouldTerminate(TAlgorithmState algorithmState,
    IReadOnlyList<InnerShouldTerminate> innerTerminators,
    TSearchSpace searchSpace, TProblem problem);
}
