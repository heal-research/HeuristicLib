using Generator.Equals;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

[Equatable]
public abstract partial record CompositeTerminator<TGenotype, TAlgorithmState, TSearchSpace, TProblem, TState>
  : ITerminator<TGenotype, TAlgorithmState, TSearchSpace, TProblem>
  where TAlgorithmState : class, IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  [OrderedEquality] protected ImmutableArray<ITerminator<TGenotype, TAlgorithmState, TSearchSpace, TProblem>> InnerTerminators { get; }
  
  protected CompositeTerminator(ImmutableArray<ITerminator<TGenotype, TAlgorithmState, TSearchSpace, TProblem>> innerTerminators)
  {
    InnerTerminators = innerTerminators;
  }
  
  public ITerminatorInstance<TGenotype, TAlgorithmState, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new Instance(this, InnerTerminators.Select(instanceRegistry.Resolve).ToArray(), CreateInitialState());
  
  protected abstract TState CreateInitialState();
  
  protected abstract bool ShouldTerminate(TAlgorithmState algorithmState, TState state,
    IReadOnlyList<ITerminatorInstance<TGenotype, TAlgorithmState, TSearchSpace, TProblem>> innerTerminators,
    TSearchSpace searchSpace, TProblem problem);
  
  private sealed class Instance(CompositeTerminator<TGenotype, TAlgorithmState, TSearchSpace, TProblem, TState> composite,
    IReadOnlyList<ITerminatorInstance<TGenotype, TAlgorithmState, TSearchSpace, TProblem>> innerTerminators,
    TState terminatorState)
    : ITerminatorInstance<TGenotype, TAlgorithmState, TSearchSpace, TProblem>
  {
    public bool ShouldTerminate(TAlgorithmState state, TSearchSpace searchSpace, TProblem problem)
    {
      return composite.ShouldTerminate(state, terminatorState, innerTerminators, searchSpace, problem);
    }
  }
}
