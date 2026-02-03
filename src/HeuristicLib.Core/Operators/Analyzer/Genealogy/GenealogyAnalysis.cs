using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Collections;
using HEAL.HeuristicLib.Operators.Crossover;
using HEAL.HeuristicLib.Operators.Prototypes;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Analyzer.Genealogy;

public static class GenealogyAnalysis {
  public static GenealogyAnalysis<TGenotype> Create<TGenotype, TE, TP, TRes>(IAlgorithmBuilder<TGenotype, TE, TP, TRes> prototype, IEqualityComparer<TGenotype>? equality = null, bool saveSpace = false)
    where TE : class, IEncoding<TGenotype>
    where TP : class, IProblem<TGenotype, TE>
    where TRes : PopulationAlgorithmState<TGenotype>
    where TGenotype : class {
    var t = new GenealogyAnalysis<TGenotype>(equality, saveSpace);
    t.AttachTo(prototype);
    return t;
  }
}

public class GenealogyAnalysis<T>(IEqualityComparer<T>? equality = null, bool saveSpace = false) : AttachedAnalysis<T, PopulationAlgorithmState<T>>
  where T : class {
  public readonly GenealogyGraph<T> Graph = new(equality ?? EqualityComparer<T>.Default);

  public override void AfterCrossover(IReadOnlyList<T> res, IReadOnlyList<IParents<T>> parents, IRandomNumberGenerator random, IEncoding<T> searchSpace, IProblem<T, IEncoding<T>> problem) {
    foreach (var (parents1, child) in parents.Zip(res))
      Graph.AddConnection([parents1.Item1, parents1.Item2], child);
  }

  public override void AfterMutation(IReadOnlyList<T> res, IReadOnlyList<T> parent, IRandomNumberGenerator random, IEncoding<T> searchSpace, IProblem<T, IEncoding<T>> problem) {
    foreach (var (parents1, child) in parent.Zip(res))
      Graph.AddConnection([parents1], child);
  }

  public override void AfterInterception(PopulationAlgorithmState<T> currentAlgorithmState, PopulationAlgorithmState<T>? previousIterationResult, IEncoding<T> searchSpace, IProblem<T, IEncoding<T>> problem) {
    var ordered = currentAlgorithmState.Population.OrderBy(x => x.ObjectiveVector, problem.Objective.TotalOrderComparer).ToArray();
    Graph.SetAsNewGeneration(ordered.Select(x => x.Genotype), saveSpace);
  }
}

public class RankAnalysis<T>(IEqualityComparer<T>? equality = null) : GenealogyAnalysis<T>(equality) where T : class {
  public List<List<double>> Ranks { get; } = [];

  public override void AfterInterception(PopulationAlgorithmState<T> currentAlgorithmState, PopulationAlgorithmState<T>? previousIterationResult, IEncoding<T> searchSpace, IProblem<T, IEncoding<T>> problem) {
    base.AfterInterception(currentAlgorithmState, previousIterationResult, searchSpace, problem);
    RecordRanks(Graph, Ranks);
  }

  private static void RecordRanks<TGenotype>(GenealogyGraph<TGenotype> graph, List<List<double>> ranks) where TGenotype : notnull {
    if (graph.Nodes.Count < 2)
      return;
    var line = graph.Nodes[^2].Values
                    .Where(x => x.Layer == 0)
                    .OrderBy(x => x.Rank)
                    .Select(node => node.GetAllDescendants().Where(x => x.Rank >= 0).AverageOrNaN(x => x.Rank))
                    .ToList();
    if (line.Count > 0) {
      ranks.Add(line);
    }
  }
}
