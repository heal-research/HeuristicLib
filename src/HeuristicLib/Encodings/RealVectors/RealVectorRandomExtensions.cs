using HEAL.HeuristicLib.Encodings.Vectors;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Encodings.RealVectors;

public static class RealVectorRandomExtensions
{
    extension(IRandomNumberGenerator random)
    {
        public RealVector NextRealVectorUniform(RealVector minimum, RealVector maximum, int length)
        {
            if (!Vector.AreBroadcastableTo(length, minimum, maximum))
                throw new ArgumentException("Vectors must be compatible for broadcasting.");

            var result = new double[length];
            for (var i = 0; i < length; i++)
            {
                var low = minimum.Count == 1 ? minimum[0] : minimum[i];
                var high = maximum.Count == 1 ? maximum[0] : maximum[i];
                result[i] = random.NextDouble(low, high);
            }

            return RealVector.FromOwnedArray(result);
        }

        public RealVector NextRealVectorNormal(RealVector mean, RealVector std, int length)
        {
            if (!Vector.AreBroadcastableTo(length, mean, std))
                throw new ArgumentException("Vectors must be compatible for broadcasting.");

            var result = new double[length];
            for (var i = 0; i < length; i++)
            {
                var mu = mean.Count == 1 ? mean[0] : mean[i];
                var sigma = std.Count == 1 ? std[0] : std[i];
                result[i] = random.NextNormal(mu, sigma);
            }

            return RealVector.FromOwnedArray(result);
        }
    }
}
