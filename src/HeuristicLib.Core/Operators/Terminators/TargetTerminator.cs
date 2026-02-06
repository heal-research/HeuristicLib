using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

public class TargetTerminator<TGenotype>(ObjectiveVector target)
  : Terminator<TGenotype, PopulationState<TGenotype>, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
{
  public override Func<PopulationState<TGenotype>, bool> CreateShouldTerminatePredicate(ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem)
  {
    return currentIterationState => currentIterationState.Population.Any(x => !target.Dominates(x.ObjectiveVector, problem.Objective));
  }
}
