using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

public class AllTerminator<TGenotype, TAlgorithmState, TSearchSpace, TProblem> : Terminator<TGenotype, TAlgorithmState, TSearchSpace, TProblem>
  where TGenotype : class
  where TAlgorithmState : class, IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  private readonly IReadOnlyList<ITerminator<TGenotype, TAlgorithmState, TSearchSpace, TProblem>> terminators;

  public AllTerminator(params IReadOnlyList<ITerminator<TGenotype, TAlgorithmState, TSearchSpace, TProblem>> terminators)
  {
    this.terminators = terminators;
  }

  public override Instance CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
  {
    var terminatorInstances = terminators.Select(instanceRegistry.GetOrCreate).ToList();
    return new Instance(terminatorInstances);
  }

  public class Instance
    : TerminatorInstance<TGenotype, TAlgorithmState, TSearchSpace, TProblem>
  {
    private readonly IReadOnlyList<ITerminatorInstance<TGenotype, TAlgorithmState, TSearchSpace, TProblem>> terminators;
    
    public Instance(IReadOnlyList<ITerminatorInstance<TGenotype, TAlgorithmState, TSearchSpace, TProblem>> terminators) {
      this.terminators = terminators;
    }

    public override bool ShouldTerminate(TAlgorithmState state, TSearchSpace searchSpace, TProblem problem)
    {
      return terminators.All(t => t.ShouldTerminate(state, searchSpace, problem));
    }
  }
}
