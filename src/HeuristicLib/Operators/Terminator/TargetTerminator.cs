using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Operators.Terminator;

public class TargetTerminator<TGenotype>(ObjectiveVector target) : Terminator<TGenotype, PopulationIterationResult<TGenotype>, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>> {
  public override bool ShouldTerminate(PopulationIterationResult<TGenotype> currentIterationState,
                                       PopulationIterationResult<TGenotype>? previousIterationState,
                                       IEncoding<TGenotype> encoding,
                                       IProblem<TGenotype, IEncoding<TGenotype>> problem) =>
    currentIterationState.Population.Any(x => !target.Dominates(x.ObjectiveVector, problem.Objective));
}
