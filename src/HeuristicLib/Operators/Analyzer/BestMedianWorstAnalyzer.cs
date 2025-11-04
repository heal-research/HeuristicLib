using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Operators.Analyzer;

public record BestMedianWorstEntry<TGenotype>(Solution<TGenotype> Best, Solution<TGenotype> Median, Solution<TGenotype> Worst);

public class BestMedianWorstAnalyzer<TGenotype> : IAnalyzer<TGenotype, PopulationIterationResult<TGenotype>, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>> {
  public readonly List<BestMedianWorstEntry<TGenotype>> CurrentState = [];

  public void Analyze(PopulationIterationResult<TGenotype> currentIterationResult, PopulationIterationResult<TGenotype>? previousIterationResult, IEncoding<TGenotype> encoding, IProblem<TGenotype, IEncoding<TGenotype>> problem) {
    var ordered = currentIterationResult.Population.OrderBy(x => x.ObjectiveVector, problem.Objective.TotalOrderComparer).ToArray();
    CurrentState.Add(new BestMedianWorstEntry<TGenotype>(ordered[0], ordered[ordered.Length / 2], ordered[^1]));
  }
}
