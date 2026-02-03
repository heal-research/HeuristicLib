using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Terminators;

public class TargetTerminator<TGenotype>(ObjectiveVector target) : Terminator<TGenotype, PopulationAlgorithmState<TGenotype>, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
{
  public override bool ShouldTerminate(PopulationAlgorithmState<TGenotype> currentIterationState,
    PopulationAlgorithmState<TGenotype>? previousIterationState,
    ISearchSpace<TGenotype> searchSpace,
    IProblem<TGenotype, ISearchSpace<TGenotype>> problem) =>
    currentIterationState.Population.Any(x => !target.Dominates(x.ObjectiveVector, problem.Objective));
}
