using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Genotypes.Vectors;

public static class IntegerVectorRandomExtensions
{
  extension(IRandomNumberGenerator random)
  {
    public IntegerVector NextIntVector(IntegerVector minimum, IntegerVector maximum, int length)
    {
      if (minimum.Count != 1 && minimum.Count != length)
        throw new ArgumentException("Minimum vector must be broadcast-compatible with the requested length.", nameof(minimum));
      if (maximum.Count != 1 && maximum.Count != length)
        throw new ArgumentException("Maximum vector must be broadcast-compatible with the requested length.", nameof(maximum));
      if (!(minimum <= maximum).All())
        throw new ArgumentException("Minimum values must be less than or equal to maximum values.");

      var result = new int[length];
      for (var dim = 0; dim < length; dim++) {
        result[dim] = random.NextInt(minimum, maximum, dim);
      }

      return result;
    }

    public int NextInt(IntegerVector minimum, IntegerVector maximum, int dim)
    {
      int low = minimum.Count == 1 ? minimum[0] : minimum[dim];
      int high = maximum.Count == 1 ? maximum[0] : maximum[dim];
      return random.NextInt(low, high, true);
    }
  }
}