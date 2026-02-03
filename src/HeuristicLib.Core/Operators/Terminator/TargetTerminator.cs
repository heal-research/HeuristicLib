using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Operators.Terminator;

public class TargetTerminator<TGenotype>(ObjectiveVector target) : Terminator<TGenotype, PopulationAlgorithmState<TGenotype>, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>> {
  public override bool ShouldTerminate(PopulationAlgorithmState<TGenotype> currentIterationState,
                                       PopulationAlgorithmState<TGenotype>? previousIterationState,
                                       IEncoding<TGenotype> searchSpace,
                                       IProblem<TGenotype, IEncoding<TGenotype>> problem) =>
    currentIterationState.Population.Any(x => !target.Dominates(x.ObjectiveVector, problem.Objective));
}
