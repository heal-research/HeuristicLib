using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Operators;

public static class MultiMutator {
  public static MultiMutator<TGenotype, TEncoding, TProblem> Create<TGenotype, TEncoding, TProblem>(IReadOnlyList<IMutator<TGenotype, TEncoding, TProblem>> mutators, IReadOnlyList<double>? weights = null) where TEncoding : class, IEncoding<TGenotype> where TProblem : class, IProblem<TGenotype, TEncoding> {
    return new MultiMutator<TGenotype, TEncoding, TProblem>(mutators, weights);
  }

  public static MultiMutator<TGenotype, TEncoding, TProblem> Create<TGenotype, TEncoding, TProblem>(params IMutator<TGenotype, TEncoding, TProblem>[] mutators) where TEncoding : class, IEncoding<TGenotype> where TProblem : class, IProblem<TGenotype, TEncoding> {
    return new MultiMutator<TGenotype, TEncoding, TProblem>(mutators, null);
  }

  public static MultiMutator<TGenotype, TEncoding, TProblem> WithRate<TGenotype, TEncoding, TProblem>(this IMutator<TGenotype, TEncoding, TProblem> mutator, double mutationRate) where TEncoding : class, IEncoding<TGenotype> where TProblem : class, IProblem<TGenotype, TEncoding> {
    return Create([mutator, NoChangeMutator<TGenotype>.Instance], [mutationRate, 1 - mutationRate]);
  }
}
