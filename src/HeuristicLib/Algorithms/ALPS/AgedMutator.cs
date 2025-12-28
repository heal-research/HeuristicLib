using HEAL.HeuristicLib.Operators.Mutator;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms.ALPS;

public class AgedMutator<TGenotype, TSearchSpace, TProblem>(IMutator<TGenotype, TSearchSpace, TProblem> internalMutator) : IMutator<AgedGenotype<TGenotype>, AgedSearchSpace<TGenotype, TSearchSpace>, AgedProblem<TGenotype, TSearchSpace, TProblem>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace> {
  public IReadOnlyList<AgedGenotype<TGenotype>> Mutate(IReadOnlyList<AgedGenotype<TGenotype>> population, IRandomNumberGenerator random, AgedSearchSpace<TGenotype, TSearchSpace> searchSpace, AgedProblem<TGenotype, TSearchSpace, TProblem> problem) {
    var innerPopulation = new TGenotype[population.Count];
    for (int i = 0; i < population.Count; i++) {
      innerPopulation[i] = population[i].InnerGenotype;
    }

    var mutated = internalMutator.Mutate(innerPopulation, random, searchSpace.InnerSearchSpace, problem.InnerProblem);
    var result = new AgedGenotype<TGenotype>[mutated.Count];
    for (int i = 0; i < mutated.Count; i++) {
      // Find the original Solution to get the age
      var originalISolution = population.Single(s => Equals(s.InnerGenotype, mutated[i]));
      result[i] = originalISolution;
    }

    return result;
  }
}
