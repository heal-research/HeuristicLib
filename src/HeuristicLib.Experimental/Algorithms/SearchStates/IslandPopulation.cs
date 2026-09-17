using System.Collections;
using HEAL.HeuristicLib.Objectives;

namespace HEAL.HeuristicLib.Algorithms;

public record IslandPopulation<TCandidate> : IEnumerable<EvaluatedCandidate<TCandidate>>
{
    public ValueArray<Population<TCandidate>> Islands { get; init; }

    public IslandPopulation(IReadOnlyList<Population<TCandidate>> islands)
    {
        Islands = islands.ToValueArray();
    }


    public IEnumerator<EvaluatedCandidate<TCandidate>> GetEnumerator() => Islands.SelectMany(island => island.EvaluatedCandidates).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
