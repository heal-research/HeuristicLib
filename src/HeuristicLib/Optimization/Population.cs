using System.Collections;
using Generator.Equals;

namespace HEAL.HeuristicLib.Optimization;

public static class Population
{
    public static Population<TGenotype> From<TGenotype>(IEnumerable<TGenotype> genotypes, IEnumerable<ObjectiveVector> fitnesses) => new([.. genotypes.Zip(fitnesses, Solution.From)]);

    public static Population<TGenotype> From<TGenotype>(IEnumerable<Solution<TGenotype>> solutions) => new([.. solutions]);
}

[Equatable]
public partial record Population<TGenotype> : ISolutionLayout<TGenotype>
{
    [OrderedEquality]
    public ImmutableArray<Solution<TGenotype>> Solutions { get; init; }

    public IEnumerable<TGenotype> Genotypes => Solutions.Select(x => x.Genotype);

    public Population(params ImmutableArray<Solution<TGenotype>> Solutions)
    {
        this.Solutions = Solutions;
    }

    public IEnumerator<Solution<TGenotype>> GetEnumerator() => Solutions.AsReadOnly().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
