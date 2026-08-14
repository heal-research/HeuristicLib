using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Genotypes.Vectors;

public static class IntegerVectorRandomExtensions
{
    extension(IRandomNumberGenerator random)
    {
        public IntegerVector NextIntegerVectorUniform(IntegerVector minimum, IntegerVector maximum, int length)
        {
            if (!Vector.AreBroadcastableTo(length, minimum, maximum))
                throw new ArgumentException("Bounds must be broadcast-compatible with the requested length.");

            var result = new int[length];
            for (var dim = 0; dim < length; dim++)
            {
                result[dim] = random.NextIntegerVectorUniformAt(minimum, maximum, dim);
            }

            return IntegerVector.FromOwnedArray(result);
        }

        public IntegerVector NextIntegerVectorNormal(RealVector mean, RealVector std, IntegerVector minimum, IntegerVector maximum, int length)
          => random.NextRealVectorNormal(mean, std, length).RoundToIntegerVector(minimum, maximum);

        public int NextIntegerVectorUniformAt(IntegerVector minimum, IntegerVector maximum, int dim)
        {
            int low = minimum.Count == 1 ? minimum[0] : minimum[dim];
            int high = maximum.Count == 1 ? maximum[0] : maximum[dim];
            return random.NextInt(low, high, inclusiveHigh: true);
        }
    }
}
