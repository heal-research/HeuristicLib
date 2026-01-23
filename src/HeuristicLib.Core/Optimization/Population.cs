using System.Collections;
using HEAL.HeuristicLib.Collections;

namespace HEAL.HeuristicLib.Optimization;

public static class Population
{
  public static Population<TGenotype> From<TGenotype>(IReadOnlyList<TGenotype> genotypes, IReadOnlyList<ObjectiveVector> fitnesses) => new(genotypes, fitnesses);

  public static Population<TGenotype> From<TGenotype>(IEnumerable<ISolution<TGenotype>> solutions) => new(new ImmutableList<ISolution<TGenotype>>(solutions));
}

public record Population<TGenotype>(ImmutableList<ISolution<TGenotype>> Solutions) : IISolutionLayout<TGenotype>
{
  public Population(params IEnumerable<ISolution<TGenotype>> solutions)
    : this(new ImmutableList<ISolution<TGenotype>>(solutions))
  {
  }

  public Population(IReadOnlyList<TGenotype> genotypes, IReadOnlyList<ObjectiveVector> fitnesses)
    : this(ToISolutions(genotypes, fitnesses))
  {
  }

  private static ImmutableList<ISolution<TGenotype>> ToISolutions(IReadOnlyList<TGenotype> genotypes, IReadOnlyList<ObjectiveVector> fitnesses)
  {
    if (genotypes.Count != fitnesses.Count) {
      throw new ArgumentException("Genotypes and fitnesses must have the same length.");
    }

    var solutions = genotypes.Zip(fitnesses).Select(x => Solution.From(x.First, x.Second));
    return new ImmutableList<ISolution<TGenotype>>(solutions);
  }

  public IEnumerator<ISolution<TGenotype>> GetEnumerator() => Solutions.GetEnumerator();

  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
  public IEnumerable<TGenotype> Genotypes => Solutions.Select(x => x.Genotype);
}
