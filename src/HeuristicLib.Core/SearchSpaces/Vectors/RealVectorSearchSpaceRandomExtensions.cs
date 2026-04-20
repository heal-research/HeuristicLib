using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.SearchSpaces.Vectors;

public static class RealVectorSearchSpaceRandomExtensions
{
  extension(IRandomNumberGenerator random)
  {
    public RealVector NextRealVectorUniform(RealVectorSearchSpace searchSpace, RealVector? minimum = null, RealVector? maximum = null)
    {
      if (minimum is not null && (minimum < searchSpace.Minimum).Any()) {
        throw new ArgumentException("Minimum values must be greater or equal to searchSpace minimum values");
      }

      if (maximum is not null && (maximum > searchSpace.Maximum).Any()) {
        throw new ArgumentException("Maximum values must be less or equal to searchSpace maximum values");
      }

      return random.NextRealVectorUniform(minimum ?? searchSpace.Minimum, maximum ?? searchSpace.Maximum, searchSpace.Length);
    }

    public RealVector NextRealVectorNormal(RealVectorSearchSpace searchSpace, RealVector means, RealVector sigmas)
      => RealVector.Clamp(random.NextRealVectorNormal(means, sigmas, searchSpace.Length), searchSpace.Minimum, searchSpace.Maximum);
  }
}