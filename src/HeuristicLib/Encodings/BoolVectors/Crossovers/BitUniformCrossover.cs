using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Encodings.BoolVectors;

/// <summary>
/// Takes each element from one parent chosen at random, so the child inherits position by position rather than in
/// contiguous runs.
/// </summary>
public record BitUniformCrossover : SingleCandidateCrossover<BoolVector, BoolVectorSearchSpace>, IInvariantContract<BoolVector>
{
    /// <remarks>
    /// Every element comes from a parent at the same position, so the length is whatever the parents share. Cardinality
    /// is not: a child can take set elements from either side and end up outside both parents' counts.
    /// </remarks>
    public bool? Ensures(ISearchInvariant<BoolVector> invariant) => invariant switch
    {
        BoolVectorLength => true,
        BoolVectorCardinality => false,
        _ => null
    };

    public override BoolVector CrossParents(Parents<BoolVector> parents, IRandomNumberGenerator random, BoolVectorSearchSpace searchSpace) =>
        Cross(random, [parents.Parent1, parents.Parent2]);

    /// <summary>
    /// Builds a child whose length is the shortest parent's, taking each element from a uniformly chosen parent.
    /// </summary>
    public static BoolVector Cross(IRandomNumberGenerator random, IReadOnlyList<BoolVector> parents)
    {
        ArgumentOutOfRangeException.ThrowIfZero(parents.Count);

        var length = parents.Min(parent => parent.Count);
        var elements = new bool[length];
        for (var i = 0; i < length; i++)
        {
            elements[i] = parents[random.NextInt(parents.Count)][i];
        }

        return BoolVector.FromOwnedArray(elements);
    }
}
