using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.Random.Distributions;

namespace HEAL.HeuristicLib.Genotypes.Vectors;

public static class RealVectorRandomExtensions
{
  extension(IRandomNumberGenerator random)
  {
    public RealVector NextDouble(RealVector minimum, RealVector maximum, int length)
    {
      if (!RealVector.AreCompatible(length, minimum, maximum))
        throw new ArgumentException("Vectors must be compatible for broadcasting.");

      var value = new RealVector(random.NextDoubles(length));
      return minimum + ((maximum - minimum) * value);
    }

    public RealVector NextGaussian(RealVector mean, RealVector std, int length)
    {
      if (!RealVector.AreCompatible(length, mean, std))
        throw new ArgumentException("Vectors must be compatible for broadcasting.");

      var result = new double[length];
      for (var i = 0; i < length; i++) {
        var mu = mean.Count == 1 ? mean[0] : mean[i];
        var sigma = std.Count == 1 ? std[0] : std[i];
        result[i] = random.NextGaussian(mu, sigma);
      }

      return result;
    }
  }
}