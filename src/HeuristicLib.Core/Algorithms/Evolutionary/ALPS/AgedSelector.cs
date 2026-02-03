using HEAL.HeuristicLib.Operators.Selector;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Algorithms.ALPS;

public class AgedSelector<TGenotype, TEncoding, TProblem>(ISelector<TGenotype, TEncoding, TProblem> internalSelector) : ISelector<AgedGenotype<TGenotype>, AgedSearchSpace<TGenotype, TEncoding>, AgedProblem<TGenotype, TEncoding, TProblem>>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  public IReadOnlyList<ISolution<AgedGenotype<TGenotype>>> Select(IReadOnlyList<ISolution<AgedGenotype<TGenotype>>> population, Objective objective, int count, IRandomNumberGenerator random, AgedSearchSpace<TGenotype, TEncoding> searchSpace, AgedProblem<TGenotype, TEncoding, TProblem> problem) {
    var innerPopulation = new ISolution<TGenotype>[population.Count];
    for (var i = 0; i < population.Count; i++) {
      innerPopulation[i] = new Solution<TGenotype>(population[i].Genotype.InnerGenotype, population[i].ObjectiveVector);
    }

    var selected = internalSelector.Select(innerPopulation, objective, count, random, searchSpace.InnerEncoding, problem.InnerProblem);
    var result = new ISolution<AgedGenotype<TGenotype>>[selected.Count];
    for (var i = 0; i < selected.Count; i++) {
      // Find the original Solution to get the age
      var originalISolution = population.Single(s => Equals(s.Genotype.InnerGenotype, selected[i].Genotype));
      result[i] = originalISolution;
    }

    return result;
  }
}
