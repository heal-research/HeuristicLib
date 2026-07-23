using HEAL.HeuristicLib.Genotypes.Vectors;

namespace HEAL.HeuristicLib.APIs.TreeSearchLib;

public readonly struct IntegerVectorResolver : IDecisionResolver<IntegerVector, int>
{
    public IntegerVector Resolve(IEnumerable<int> choices) => new(choices);
}
