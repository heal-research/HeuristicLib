using System.Collections;
using Generator.Equals;

namespace HEAL.HeuristicLib.Optimization;

[Equatable]
public partial record IslandPopulation<TGenotype> : ISolutionLayout<TGenotype>
{
    [OrderedEquality]
    public IReadOnlyList<Population<TGenotype>> Islands { get; init; }

    public IslandPopulation(IReadOnlyList<Population<TGenotype>> Islands)
    {
        this.Islands = Islands;
    }
    public IEnumerator<Solution<TGenotype>> GetEnumerator() => Islands.SelectMany(island => island.Solutions).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
