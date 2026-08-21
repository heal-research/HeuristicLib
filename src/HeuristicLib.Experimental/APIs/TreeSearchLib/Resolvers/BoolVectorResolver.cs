using HEAL.HeuristicLib.Encodings.BoolVectors;

namespace HEAL.HeuristicLib.APIs.TreeSearchLib;

public readonly struct BoolVectorResolver : IDecisionResolver<BoolVector, bool>
{
    public BoolVector Resolve(IEnumerable<bool> choices) => new(choices);
}
