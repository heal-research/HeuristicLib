using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Operators.Creators.RealVectorCreators;

public record UniformDistributedCreator : SingleCandidateCreator<RealVector, RealVectorSearchSpace>
{
    public UniformDistributedCreator() { }

    /// <summary>
    /// Pins the bounds to those of <paramref name="searchSpace"/> instead of taking them from the search space
    /// supplied at execution time.
    /// </summary>
    public UniformDistributedCreator(RealVectorSearchSpace searchSpace)
    {
        Minimum = searchSpace.Minimum;
        Maximum = searchSpace.Maximum;
    }

    /// <summary>
    /// Lower bounds overriding those of the search space supplied at execution time. Leave unset to use the search
    /// space bounds.
    /// </summary>
    public RealVector? Minimum { get; init; }

    /// <summary>
    /// Upper bounds overriding those of the search space supplied at execution time. Leave unset to use the search
    /// space bounds.
    /// </summary>
    public RealVector? Maximum { get; init; }

    public override RealVector CreateCandidate(IRandomNumberGenerator random, RealVectorSearchSpace searchSpace) =>
        Create(random, searchSpace, Minimum, Maximum);

    public static RealVector Create(IRandomNumberGenerator random, int length, RealVector minimum, RealVector maximum) =>
        random.NextRealVectorUniform(minimum, maximum, length);

    public static RealVector Create(IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, RealVector? minimum = null, RealVector? maximum = null) =>
        random.NextRealVectorUniform(searchSpace, minimum, maximum);
}
