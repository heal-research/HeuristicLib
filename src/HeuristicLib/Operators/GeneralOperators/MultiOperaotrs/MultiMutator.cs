using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Operators;

public class MultiMutator {
  public static MultiMutator<TGenotype, TEncoding, TProblem> Create<TGenotype, TEncoding, TProblem>(IReadOnlyList<IMutator<TGenotype, TEncoding, TProblem>> mutators, IReadOnlyList<double>? weights = null) where TEncoding : class, IEncoding<TGenotype> where TProblem : class, IProblem<TGenotype, TEncoding> {
    return new MultiMutator<TGenotype, TEncoding, TProblem>(mutators, weights);
  }
}
