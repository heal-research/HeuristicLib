using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Algorithms.ALPS;

public class AgedProblem<TGenotype, TEncoding, TProblem>(TProblem innerProblem) : IProblem<AgedGenotype<TGenotype>, AgedEncoding<TGenotype, TEncoding>>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  public TProblem InnerProblem { get; } = innerProblem;

  public Objective Objective => InnerProblem.Objective;

  public AgedEncoding<TGenotype, TEncoding> SearchSpace { get; } = new(innerProblem.SearchSpace);

  public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<AgedGenotype<TGenotype>> solutions) {
    var genotypes = new TGenotype[solutions.Count];
    for (int i = 0; i < solutions.Count; i++) {
      genotypes[i] = solutions[i].InnerGenotype;
    }

    return InnerProblem.Evaluate(genotypes);
  }
}
