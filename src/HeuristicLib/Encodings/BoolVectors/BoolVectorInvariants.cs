using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Encodings.BoolVectors;

/// <summary>
/// The candidate has the given number of elements.
/// </summary>
public sealed record BoolVectorLength(int Length) : ICandidateInvariant<BoolVector>
{
    public string Name => $"Length({Length})";

    public bool Holds(BoolVector candidate) => candidate.Count == Length;
}

/// <summary>
/// The candidate has exactly the given number of set elements.
/// </summary>
/// <remarks>
/// This implies <see cref="BoolVectorMinimumSetElements"/> for any smaller or equal minimum, so an operator that only
/// needs "at least two set elements" may be used over a space that fixes the count at two or more.
/// </remarks>
public sealed record BoolVectorCardinality(int Cardinality) : ICandidateInvariant<BoolVector>
{
    public string Name => $"Cardinality({Cardinality})";

    public bool Holds(BoolVector candidate) => CountSetElements(candidate) == Cardinality;

    public bool Implies(ICandidateInvariant<BoolVector> other) => other switch
    {
        BoolVectorCardinality cardinality => cardinality.Cardinality == Cardinality,
        BoolVectorMinimumSetElements minimum => Cardinality >= minimum.Minimum,
        _ => false
    };

    internal static int CountSetElements(BoolVector candidate)
    {
        var count = 0;
        for (var i = 0; i < candidate.Count; i++)
        {
            if (candidate[i])
            {
                count++;
            }
        }

        return count;
    }
}

/// <summary>
/// The candidate has at least the given number of set elements.
/// </summary>
public sealed record BoolVectorMinimumSetElements(int Minimum) : ICandidateInvariant<BoolVector>
{
    public string Name => $"AtLeastSet({Minimum})";

    public bool Holds(BoolVector candidate) => BoolVectorCardinality.CountSetElements(candidate) >= Minimum;

    public bool Implies(ICandidateInvariant<BoolVector> other) =>
        other is BoolVectorMinimumSetElements minimum && Minimum >= minimum.Minimum;
}
