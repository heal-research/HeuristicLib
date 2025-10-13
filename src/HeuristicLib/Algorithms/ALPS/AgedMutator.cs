using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Algorithms.ALPS;

public class AgedMutator<TGenotype, TEncoding, TProblem>(IMutator<TGenotype, TEncoding, TProblem> internalMutator) : IMutator<AgedGenotype<TGenotype>, AgedEncoding<TGenotype, TEncoding>, AgedProblem<TGenotype, TEncoding, TProblem>>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  public IReadOnlyList<AgedGenotype<TGenotype>> Mutate(IReadOnlyList<AgedGenotype<TGenotype>> population, IRandomNumberGenerator random, AgedEncoding<TGenotype, TEncoding> encoding, AgedProblem<TGenotype, TEncoding, TProblem> problem) {
    var innerPopulation = new TGenotype[population.Count];
    for (int i = 0; i < population.Count; i++) {
      innerPopulation[i] = population[i].InnerGenotype;
    }

    var mutated = internalMutator.Mutate(innerPopulation, random, encoding.InnerEncoding, problem.InnerProblem);
    var result = new AgedGenotype<TGenotype>[mutated.Count];
    for (int i = 0; i < mutated.Count; i++) {
      // Find the original solution to get the age
      var originalSolution = population.Single(s => Equals(s.InnerGenotype, mutated[i]));
      result[i] = originalSolution;
    }

    return result;
  }
}
