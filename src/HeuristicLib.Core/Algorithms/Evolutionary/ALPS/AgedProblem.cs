using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Algorithms.ALPS;

public class AgedProblem<TGenotype, TEncoding, TProblem>(TProblem innerProblem) : IProblem<AgedGenotype<TGenotype>, AgedSearchSpace<TGenotype, TEncoding>>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  public TProblem InnerProblem { get; } = innerProblem;

  public Objective Objective => InnerProblem.Objective;
  public ObjectiveVector Evaluate(AgedGenotype<TGenotype> solution, IRandomNumberGenerator random) => InnerProblem.Evaluate(solution.InnerGenotype, random);

  public AgedSearchSpace<TGenotype, TEncoding> SearchSpace { get; } = new(innerProblem.SearchSpace);

  //public IReadOnlyList<ObjectiveVector> PredictAndTrain(IReadOnlyList<AgedGenotype<TGenotype>> ISolutions) {
  //  var genotypes = new TGenotype[ISolutions.Count];
  //  for (int i = 0; i < ISolutions.Count; i++) {
  //    genotypes[i] = ISolutions[i].InnerGenotype;
  //  }

  //  return InnerProblem.PredictAndTrain(genotypes);
  //}
}
