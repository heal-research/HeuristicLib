using System.Collections;
using HEAL.HeuristicLib.Core;

namespace HEAL.HeuristicLib.Optimization;

public record Population<TGenotype>(ImmutableList<Solution<TGenotype>> Solutions) : ISolutionLayout<TGenotype> {
  public IEnumerator<Solution<TGenotype>> GetEnumerator() => Solutions.GetEnumerator();

  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

  public static Population<TGenotype> From(IReadOnlyList<TGenotype> genotypes, IReadOnlyList<ObjectiveVector> fitnesses) {
    if (genotypes.Count != fitnesses.Count) throw new ArgumentException("Genotypes and fitnesses must have the same length.");

    var solutions = Enumerable.Zip(genotypes, fitnesses)
                              .Select(x => Solution.From(x.First, x.Second));
    return new Population<TGenotype>(new ImmutableList<Solution<TGenotype>>(solutions));
  }
}
