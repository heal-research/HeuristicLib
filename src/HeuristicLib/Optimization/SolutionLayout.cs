using HEAL.HeuristicLib.Core;

namespace HEAL.HeuristicLib.Optimization;

public static class Population {
  public static Population<TGenotype> From<TGenotype>(IReadOnlyList<TGenotype> genotypes, IReadOnlyList<ObjectiveVector> fitnesses) {
    if (genotypes.Count != fitnesses.Count) throw new ArgumentException("Genotypes and fitnesses must have the same length.");
    var solutions = Enumerable.Zip(genotypes, fitnesses).Select(x => Solution.From(x.First, x.Second));
    return new Population<TGenotype>(new ImmutableList<Solution<TGenotype>>(solutions));
  }
}
