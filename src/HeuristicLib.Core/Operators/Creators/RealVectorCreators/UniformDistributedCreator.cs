using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Operators.Creators.RealVectorCreators;

public record UniformDistributedCreator : SingleSolutionCreator<RealVector, RealVectorSearchSpace>
{
  public UniformDistributedCreator(RealVectorSearchSpace searchSpace)
    : this(searchSpace.Minimum, searchSpace.Maximum)
  { }

  public UniformDistributedCreator(RealVector? minimum = null, RealVector? maximum = null)
  {
    Minimum = minimum;
    Maximum = maximum;
  }

  public RealVector? Minimum { get; init; }
  public RealVector? Maximum { get; init; }

  public override RealVector Create(IRandomNumberGenerator random, RealVectorSearchSpace searchSpace)
    => Create(random, searchSpace, Minimum, Maximum);

  public static RealVector Create(
    IRandomNumberGenerator random,
    int length,
    RealVector minimum,
    RealVector maximum)
  {
    if (!RealVector.AreCompatible(length, minimum, maximum)) {
      throw new ArgumentException("Vectors must have compatible lengths");
    }

    return random.NextDouble(minimum, maximum, length);
  }

  public static RealVector Create(
    IRandomNumberGenerator random,
    RealVectorSearchSpace searchSpace,
    RealVector? minimum = null,
    RealVector? maximum = null)
  {
    if (minimum is not null && (minimum < searchSpace.Minimum).Any()) {
      throw new ArgumentException("Minimum values must be greater or equal to searchSpace minimum values");
    }

    if (maximum is not null && (maximum > searchSpace.Maximum).Any()) {
      throw new ArgumentException("Maximum values must be less or equal to searchSpace maximum values");
    }

    if (!RealVector.AreCompatible(searchSpace.Length, minimum ?? searchSpace.Minimum, maximum ?? searchSpace.Maximum)) {
      throw new ArgumentException("Vectors must have compatible lengths");
    }

    return Create(random, searchSpace.Length, minimum ?? searchSpace.Minimum, maximum ?? searchSpace.Maximum);
  }
}
