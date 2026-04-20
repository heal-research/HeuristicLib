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
    => random.NextRealVectorUniform(minimum, maximum, length);

  public static RealVector Create(
    IRandomNumberGenerator random,
    RealVectorSearchSpace searchSpace,
    RealVector? minimum = null,
    RealVector? maximum = null)
    => random.NextRealVectorUniform(searchSpace, minimum, maximum);
}
