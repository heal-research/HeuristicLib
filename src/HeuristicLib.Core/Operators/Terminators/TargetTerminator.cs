using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

public class TargetTerminator<TGenotype>
  : StatelessTerminator<TGenotype, PopulationState<TGenotype>, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
{
  private readonly ObjectiveVector target;
  
  public TargetTerminator(ObjectiveVector target) {
    this.target = target;
  }

  public override bool ShouldTerminate(PopulationState<TGenotype> state, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem)
  {
    return state.Population.Any(x => !target.Dominates(x.ObjectiveVector, problem.Objective));
  }
}
