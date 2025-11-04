using HEAL.HeuristicLib.Operators.Crossover;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Analyzer;

public class GenealogyGraphCrossover<T1, T2, T3>(ICrossover<T1, T2, T3> internalCrossover, GenealogyGraph<T1> graph) : ICrossover<T1, T2, T3>
  where T2 : class, IEncoding<T1>
  where T3 : class, IProblem<T1, T2>
  where T1 : notnull {
  public IReadOnlyList<T1> Cross(IReadOnlyList<(T1, T1)> parents, IRandomNumberGenerator random, T2 encoding, T3 problem) {
    var res = internalCrossover.Cross(parents, random, encoding, problem);
    foreach (var (parents1, child) in parents.Zip(res))
      graph.AddConnection([parents1.Item1, parents1.Item2], child);
    return res;
  }
}
