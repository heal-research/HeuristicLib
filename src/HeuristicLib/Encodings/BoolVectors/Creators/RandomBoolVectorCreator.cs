using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Encodings.BoolVectors;

/// <summary>
/// Draws each element independently, so the candidate has the search space's length and no other structure.
/// </summary>
/// <remarks>
/// Use <see cref="FixedCardinalityBoolVectorSearchSpace"/> with a creator of its own when a candidate has to start at
/// a given number of set elements; independent draws reach that count only by chance.
/// </remarks>
public record RandomBoolVectorCreator : SingleCandidateCreator<BoolVector, BoolVectorSearchSpace>, IInvariantContract<BoolVector>
{
    /// <summary>
    /// Gets the probability that an element is set. The expected value is in <c>[0, 1]</c>.
    /// </summary>
    public double SetProbability { get; init; } = 0.5;

    public bool? Ensures(ISearchInvariant<BoolVector> invariant) => invariant switch
    {
        BoolVectorLength length => length.Length >= 0,
        _ => null
    };

    public override BoolVector CreateCandidate(IRandomNumberGenerator random, BoolVectorSearchSpace searchSpace) =>
        Create(random, searchSpace.Length, SetProbability);

    public static BoolVector Create(IRandomNumberGenerator random, BoolVectorSearchSpace searchSpace) =>
        Create(random, searchSpace.Length, setProbability: 0.5);

    public static BoolVector Create(IRandomNumberGenerator random, int length, double setProbability = 0.5) =>
        BoolVector.FromOwnedArray(random.NextBools(length, setProbability));
}
