using System.Collections;
using Generator.Equals;

namespace HEAL.HeuristicLib.Optimization;

[Equatable]
public partial record IslandPopulation<TCandidate> : ISolutionLayout<TCandidate>
{
    [OrderedEquality]
    public ImmutableArray<Population<TCandidate>> Islands { get; init; }

    public IslandPopulation(IReadOnlyList<Population<TCandidate>> islands)
    {
        Islands = islands.ToImmutableArray();
    }
    public IEnumerator<EvaluatedCandidate<TCandidate>> GetEnumerator() => Islands.SelectMany(island => island.EvaluatedCandidates).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
