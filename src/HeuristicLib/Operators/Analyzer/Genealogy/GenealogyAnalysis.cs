using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators.Prototypes;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Analyzer.Genealogy;

public static class GenealogyAnalysis {
  public static GenealogyAnalysis<TGenotype, TE, TP, TRes> Create<TGenotype, TE, TP, TRes>(IPrototype<TGenotype, TE, TP, TRes> prototype, IEqualityComparer<TGenotype>? equality = null, bool saveSpace = false)
    where TE : class, IEncoding<TGenotype>
    where TP : class, IProblem<TGenotype, TE>
    where TRes : PopulationIterationResult<TGenotype>
    where TGenotype : class {
    var t = new GenealogyAnalysis<TGenotype, TE, TP, TRes>(equality, saveSpace);
    t.AddToProto(prototype);
    return t;
  }
}

public class GenealogyAnalysis<T, TE, TP, TRes>(IEqualityComparer<T>? equality = null, bool saveSpace = false) : SimpleAnalysis<T, TE, TP, TRes>
  where T : class where TE : class, IEncoding<T> where TP : class, IProblem<T, TE> where TRes : PopulationIterationResult<T> {
  public readonly GenealogyGraph<T> Graph = new(equality ?? EqualityComparer<T>.Default);

  protected override void AfterCrossover(IReadOnlyList<T> res, IReadOnlyList<(T, T)> parents, IRandomNumberGenerator random, TE encoding, TP problem) {
    foreach (var (parents1, child) in parents.Zip(res))
      Graph.AddConnection([parents1.Item1, parents1.Item2], child);
  }

  protected override void AfterMutation(IReadOnlyList<T> res, IReadOnlyList<T> parent, IRandomNumberGenerator random, TE encoding, TP problem) {
    foreach (var (parents1, child) in parent.Zip(res))
      Graph.AddConnection([parents1], child);
  }

  protected override void AfterInterception(TRes currentIterationResult, TRes? previousIterationResult, TE encoding, TP problem) {
    var ordered = currentIterationResult.Population.OrderBy(x => x.ObjectiveVector, problem.Objective.TotalOrderComparer).ToArray();
    Graph.SetAsNewGeneration(ordered.Select(x => x.Genotype), saveSpace);
  }
}
