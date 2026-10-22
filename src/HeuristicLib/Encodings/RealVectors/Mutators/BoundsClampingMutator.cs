using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Encodings.RealVectors;

/// <summary>
/// Clamps every coordinate of a real-vector candidate to the bounds of the supplied search space.
/// </summary>
public sealed record BoundsClampingMutator : SingleCandidateMutator<RealVector, BoundedRealVectorSearchSpace>
{
    public override RealVector MutateCandidate(
        RealVector parent,
        IRandomNumberGenerator random,
        BoundedRealVectorSearchSpace searchSpace) =>
        parent.Clamp(searchSpace.Minimum, searchSpace.Maximum);
}
