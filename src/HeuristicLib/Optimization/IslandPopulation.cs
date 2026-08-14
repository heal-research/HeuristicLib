using System.Collections;

namespace HEAL.HeuristicLib.Optimization;

public record IslandPopulation<TCandidate> : ISolutionLayout<TCandidate>
{
    public ValueArray<Population<TCandidate>> Islands { get; init; }

    public IslandPopulation(IReadOnlyList<Population<TCandidate>> islands)
    {
        Islands = islands.ToValueArray();
    }


    public IEnumerator<EvaluatedCandidate<TCandidate>> GetEnumerator() => Islands.SelectMany(island => island.EvaluatedCandidates).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
