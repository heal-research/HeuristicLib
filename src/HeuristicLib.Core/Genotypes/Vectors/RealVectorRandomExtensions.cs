using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Genotypes.Vectors;

public static class RealVectorRandomExtensions
{
  extension(IRandomNumberGenerator random)
  {
    public RealVector NextRealVectorUniform(RealVector minimum, RealVector maximum, int length)
    {
      if (!RealVector.AreCompatible(length, minimum, maximum))
        throw new ArgumentException("Vectors must be compatible for broadcasting.");

      return random.NextRealVectorUniformUnchecked(minimum, maximum, length);
    }

    public RealVector NextRealVectorNormal(RealVector mean, RealVector std, int length)
    {
      if (!RealVector.AreCompatible(length, mean, std))
        throw new ArgumentException("Vectors must be compatible for broadcasting.");

      return random.NextRealVectorNormalUnchecked(mean, std, length);
    }

    private RealVector NextRealVectorUniformUnchecked(RealVector minimum, RealVector maximum, int length)
    {
      var value = new RealVector(random.NextDoubles(length));
      return minimum + ((maximum - minimum) * value);
    }

    private RealVector NextRealVectorNormalUnchecked(RealVector mean, RealVector std, int length)
    {
      var result = new double[length];
      for (var i = 0; i < length; i++) {
        var mu = mean.Count == 1 ? mean[0] : mean[i];
        var sigma = std.Count == 1 ? std[0] : std[i];
        result[i] = random.NextNormalUnchecked(mu, sigma);
      }

      return result;
    }
  }
}