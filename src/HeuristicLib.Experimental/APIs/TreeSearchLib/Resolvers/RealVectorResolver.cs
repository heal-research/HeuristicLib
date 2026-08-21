using HEAL.HeuristicLib.Encodings.RealVectors;

namespace HEAL.HeuristicLib.APIs.TreeSearchLib;

public readonly struct RealVectorResolver : IDecisionResolver<RealVector, double>
{
    public RealVector Resolve(IEnumerable<double> choices) => new(choices);
}
