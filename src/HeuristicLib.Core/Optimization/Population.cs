using System.Collections;
using Generator.Equals;

namespace HEAL.HeuristicLib.Optimization;

public static class Population
{
  public static Population<TGenotype> From<TGenotype>(IEnumerable<TGenotype> genotypes, IEnumerable<ObjectiveVector> fitnesses) => new([..genotypes.Zip(fitnesses, Solution.From)]);

  public static Population<TGenotype> From<TGenotype>(IEnumerable<ISolution<TGenotype>> solutions) => new([..solutions]);
}

[Equatable]
public partial record Population<TGenotype> : IISolutionLayout<TGenotype>
{
  [OrderedEquality]
  public ImmutableArray<ISolution<TGenotype>> Solutions { get; init; }

  public IEnumerable<TGenotype> Genotypes => Solutions.Select(x => x.Genotype);

  public Population(params ImmutableArray<ISolution<TGenotype>> Solutions)
  {
    this.Solutions = Solutions;
  }

  private static ImmutableArray<ISolution<TGenotype>> ToSolutions(IReadOnlyList<TGenotype> genotypes, IReadOnlyList<ObjectiveVector> fitnesses)
  {
    if (genotypes.Count != fitnesses.Count) {
      throw new ArgumentException("Genotypes and fitnesses must have the same length.");
    }

    var solutions = genotypes.Zip(fitnesses).Select(x => Solution.From(x.First, x.Second));
    return [..solutions];
  }

  public IEnumerator<ISolution<TGenotype>> GetEnumerator() => Solutions.AsReadOnly().GetEnumerator();

  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
