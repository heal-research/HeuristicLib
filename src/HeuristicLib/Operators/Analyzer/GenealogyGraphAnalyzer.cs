using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Operators.Analyzer;

public class GenealogyGraphAnalyzer<T1>(GenealogyGraph<T1> graph) : IAnalyzer<T1, PopulationIterationResult<T1>, IEncoding<T1>, IProblem<T1, IEncoding<T1>>>
  where T1 : notnull {
  /// <summary>
  /// forget all but the last generation to save space
  /// </summary>
  public bool SaveSpace { get; set; } = true;

  public void Analyze(PopulationIterationResult<T1> currentIterationResult, PopulationIterationResult<T1>? previousIterationResult, IEncoding<T1> encoding, IProblem<T1, IEncoding<T1>> problem) {
    var ordered = currentIterationResult.Population.OrderBy(x => x.ObjectiveVector, problem.Objective.TotalOrderComparer).ToArray();
    graph.SetAsNewGeneration(ordered.Select(x => x.Genotype), SaveSpace);
  }
}
