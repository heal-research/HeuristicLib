using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Encodings.BoolVectors;

public record BoolVectorSearchSpace(int Length) : SearchSpace<BoolVector>
{
    public override bool Contains(BoolVector candidate) => candidate.Count == Length;
}
