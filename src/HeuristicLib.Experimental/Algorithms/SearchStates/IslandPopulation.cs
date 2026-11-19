namespace HEAL.HeuristicLib.Algorithms;

public record IslandPopulation<TCandidate>
{
    public ValueArray<Population<TCandidate>> Islands { get; init; }

    public IslandPopulation(IReadOnlyList<Population<TCandidate>> islands)
    {
        Islands = islands.ToValueArray();
    }
}
