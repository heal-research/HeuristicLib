using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Encodings.RealVectors;

public record UniformDistributedCreator : SingleCandidateCreator<RealVector, BoundedRealVectorSearchSpace>, IInvariantContract<RealVector>
{
    public UniformDistributedCreator() { }

    /// <summary>
    /// Pins the bounds to those of <paramref name="searchSpace"/> instead of taking them from the search space
    /// supplied at execution time.
    /// </summary>
    public UniformDistributedCreator(BoundedRealVectorSearchSpace searchSpace)
    {
        Minimum = searchSpace.Minimum;
        Maximum = searchSpace.Maximum;
    }

    /// <summary>
    /// Lower bounds overriding those of the search space supplied at execution time. Leave unset to use the search
    /// space bounds. See <see cref="Ensures"/> for the bounds that keep candidates inside a given search space.
    /// </summary>
    public RealVector? Minimum { get; init; }

    /// <summary>
    /// Upper bounds overriding those of the search space supplied at execution time. Leave unset to use the search
    /// space bounds. See <see cref="Ensures"/> for the bounds that keep candidates inside a given search space.
    /// </summary>
    public RealVector? Maximum { get; init; }

    /// <summary>
    /// Length comes from the search space. Bounds do too, unless both are overridden, in which case the override must
    /// lie inside the space's own bounds, which is reported when the configuration is validated.
    /// </summary>
    /// <remarks>A one sided override cannot be compared without knowing the space, so it is not checked here.</remarks>
    public bool? Ensures(ISearchInvariant<RealVector> invariant) => invariant switch
    {
        RealVectorLength => true,
        RealVectorBounds bounds when Minimum is not null && Maximum is not null => new RealVectorBounds(Minimum, Maximum).Entails(bounds),
        RealVectorBounds => true,
        _ => null
    };

    public override RealVector CreateCandidate(IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace) =>
        Create(random, searchSpace, Minimum, Maximum);

    public static RealVector Create(IRandomNumberGenerator random, int length, RealVector minimum, RealVector maximum) =>
        random.NextRealVectorUniform(minimum, maximum, length);

    public static RealVector Create(IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace, RealVector? minimum = null, RealVector? maximum = null) =>
        random.NextRealVectorUniform(searchSpace, minimum, maximum);
}
