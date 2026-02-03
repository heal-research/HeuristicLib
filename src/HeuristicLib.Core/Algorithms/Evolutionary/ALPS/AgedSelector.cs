using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms.Evolutionary.ALPS;

public class AgedSelector<TGenotype, TSearchSpace, TProblem>(ISelector<TGenotype, TSearchSpace, TProblem> internalSelector) : ISelector<AgedGenotype<TGenotype>, AgedSearchSpace<TGenotype, TSearchSpace>, AgedProblem<TGenotype, TSearchSpace, TProblem>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public IReadOnlyList<ISolution<AgedGenotype<TGenotype>>> Select(IReadOnlyList<ISolution<AgedGenotype<TGenotype>>> population, Objective objective, int count, IRandomNumberGenerator random, AgedSearchSpace<TGenotype, TSearchSpace> searchSpace, AgedProblem<TGenotype, TSearchSpace, TProblem> problem)
  {
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
