using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Encodings.IntegerVectors;

public static class IntegerVectorSearchSpaceRandomExtensions
{
    extension(IRandomNumberGenerator random)
    {
        public IntegerVector NextIntegerVectorUniform(IntegerVectorSearchSpace searchSpace) =>
            random.NextIntegerVectorUniform(searchSpace.Minimum, searchSpace.Maximum, searchSpace.Length);

        public IntegerVector NextIntegerVectorNormal(IntegerVectorSearchSpace searchSpace, RealVector means, RealVector sigmas) =>
            random.NextIntegerVectorNormal(means, sigmas, searchSpace.Minimum, searchSpace.Maximum, searchSpace.Length);
    }
}
