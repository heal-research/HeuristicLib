using HEAL.HeuristicLib.Operators.Replacer;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Algorithms.ALPS;

public class AgedReplacer<TGenotype, TEncoding, TProblem>(IReplacer<TGenotype, TEncoding, TProblem> innerReplacer) : IReplacer<AgedGenotype<TGenotype>, AgedSearchSpace<TGenotype, TEncoding>, AgedProblem<TGenotype, TEncoding, TProblem>>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  public IReadOnlyList<ISolution<AgedGenotype<TGenotype>>> Replace(IReadOnlyList<ISolution<AgedGenotype<TGenotype>>> previousPopulation, IReadOnlyList<ISolution<AgedGenotype<TGenotype>>> offspringPopulation, Objective objective, IRandomNumberGenerator random, AgedSearchSpace<TGenotype, TEncoding> searchSpace, AgedProblem<TGenotype, TEncoding, TProblem> problem) {
    var innerPreviousPopulation = new ISolution<TGenotype>[previousPopulation.Count];
    for (var i = 0; i < previousPopulation.Count; i++) {
      innerPreviousPopulation[i] = new Solution<TGenotype>(previousPopulation[i].Genotype.InnerGenotype, previousPopulation[i].ObjectiveVector);
    }

    var innerOffspringPopulation = new ISolution<TGenotype>[offspringPopulation.Count];
    for (var i = 0; i < offspringPopulation.Count; i++) {
      innerOffspringPopulation[i] = new Solution<TGenotype>(offspringPopulation[i].Genotype.InnerGenotype, offspringPopulation[i].ObjectiveVector);
    }

    var replaced = innerReplacer.Replace(innerPreviousPopulation, innerOffspringPopulation, objective, random, searchSpace.InnerEncoding, problem.InnerProblem);
    var result = new ISolution<AgedGenotype<TGenotype>>[replaced.Count];
    for (var i = 0; i < replaced.Count; i++) {
      // Find the original Solution to get the age
      var originalISolution = previousPopulation.Concat(offspringPopulation).Single(s => Equals(s.Genotype.InnerGenotype, replaced[i].Genotype));
      result[i] = originalISolution;
    }

    return result;
  }

  public int GetOffspringCount(int populationSize) {
    return innerReplacer.GetOffspringCount(populationSize);
  }
}
