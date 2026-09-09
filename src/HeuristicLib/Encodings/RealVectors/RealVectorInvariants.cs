using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Encodings.RealVectors;

/// <summary>
/// The candidate has the given number of elements.
/// </summary>
public sealed record RealVectorLength(int Length) : ISearchInvariant<RealVector>
{
    public string Name => $"Length({Length})";

    public bool IsSatisfiedBy(RealVector candidate) => candidate.Count == Length;
}

/// <summary>
/// Every element of the candidate lies within the given bounds. Each bound is a vector of length one, applying to
/// every element, or of the candidate's own length.
/// </summary>
/// <remarks>
/// Tighter bounds entail looser ones, so an operator that produces candidates within a narrower box satisfies a
/// search space with a wider one, while a narrower search space is not satisfied by a wider guarantee.
/// </remarks>
public sealed record RealVectorBounds(RealVector Minimum, RealVector Maximum) : ISearchInvariant<RealVector>
{
    public string Name => $"Bounds([{string.Join(", ", Minimum)}], [{string.Join(", ", Maximum)}])";

    public bool IsSatisfiedBy(RealVector candidate) => (candidate >= Minimum).All() && (candidate <= Maximum).All();

    public bool Entails(ISearchInvariant<RealVector> other) =>
        other is RealVectorBounds bounds && (Minimum >= bounds.Minimum).All() && (Maximum <= bounds.Maximum).All();
}
