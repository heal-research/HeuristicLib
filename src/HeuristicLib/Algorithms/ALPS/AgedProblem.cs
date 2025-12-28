using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms.ALPS;

public class AgedProblem<TGenotype, TSearchSpace, TProblem>(TProblem innerProblem) : IProblem<AgedGenotype<TGenotype>, AgedSearchSpace<TGenotype, TSearchSpace>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace> {
  public TProblem InnerProblem { get; } = innerProblem;

  public Objective Objective => InnerProblem.Objective;
  public ObjectiveVector Evaluate(AgedGenotype<TGenotype> solution, IRandomNumberGenerator random) => InnerProblem.Evaluate(solution.InnerGenotype, random);

  public AgedSearchSpace<TGenotype, TSearchSpace> SearchSpace { get; } = new(innerProblem.SearchSpace);

  //public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<AgedGenotype<TGenotype>> ISolutions) {
  //  var genotypes = new TGenotype[ISolutions.Count];
  //  for (int i = 0; i < ISolutions.Count; i++) {
  //    genotypes[i] = ISolutions[i].InnerGenotype;
  //  }

  //  return InnerProblem.Evaluate(genotypes);
  //}
}
