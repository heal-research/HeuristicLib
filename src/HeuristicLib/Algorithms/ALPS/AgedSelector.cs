using HEAL.HeuristicLib.Operators.Selector;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Algorithms.ALPS;

public class AgedSelector<TGenotype, TEncoding, TProblem>(ISelector<TGenotype, TEncoding, TProblem> internalSelector) : ISelector<AgedGenotype<TGenotype>, AgedEncoding<TGenotype, TEncoding>, AgedProblem<TGenotype, TEncoding, TProblem>>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  public IReadOnlyList<Solution<AgedGenotype<TGenotype>>> Select(IReadOnlyList<Solution<AgedGenotype<TGenotype>>> population, Objective objective, int count, IRandomNumberGenerator random, AgedEncoding<TGenotype, TEncoding> encoding, AgedProblem<TGenotype, TEncoding, TProblem> problem) {
    var innerPopulation = new Solution<TGenotype>[population.Count];
    for (int i = 0; i < population.Count; i++) {
      innerPopulation[i] = new Solution<TGenotype>(population[i].Genotype.InnerGenotype, population[i].ObjectiveVector);
    }

    var selected = internalSelector.Select(innerPopulation, objective, count, random, encoding.InnerEncoding, problem.InnerProblem);
    var result = new Solution<AgedGenotype<TGenotype>>[selected.Count];
    for (int i = 0; i < selected.Count; i++) {
      // Find the original solution to get the age
      var originalSolution = population.Single(s => Equals(s.Genotype.InnerGenotype, selected[i].Genotype));
      result[i] = originalSolution;
    }

    return result;
  }
}
