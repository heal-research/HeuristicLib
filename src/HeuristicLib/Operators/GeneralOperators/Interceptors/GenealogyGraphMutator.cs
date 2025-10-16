using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Interceptors;

public class GenealogyGraphMutator<T1, T2, T3>(IMutator<T1, T2, T3> internalMutator, GenealogyGraph<T1> graph) : IMutator<T1, T2, T3>
  where T2 : class, IEncoding<T1>
  where T3 : class, IProblem<T1, T2>
  where T1 : notnull {
  public IReadOnlyList<T1> Mutate(IReadOnlyList<T1> parent, IRandomNumberGenerator random, T2 encoding, T3 problem) {
    var res = internalMutator.Mutate(parent, random, encoding, problem);
    foreach (var (parents1, child) in parent.Zip(res))
      graph.AddConnection([parents1], child);
    return res;
  }
}
