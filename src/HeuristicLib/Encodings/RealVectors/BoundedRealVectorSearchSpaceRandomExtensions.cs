using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Encodings.RealVectors;

public static class BoundedRealVectorSearchSpaceRandomExtensions
{
    extension(IRandomNumberGenerator random)
    {
        public RealVector NextRealVectorUniform(BoundedRealVectorSearchSpace searchSpace, RealVector? minimum = null, RealVector? maximum = null)
        {
            if (minimum is not null && (minimum < searchSpace.Minimum).Any())
            {
                throw new ArgumentException("Minimum values must be greater or equal to searchSpace minimum values");
            }

            if (maximum is not null && (maximum > searchSpace.Maximum).Any())
            {
                throw new ArgumentException("Maximum values must be less or equal to searchSpace maximum values");
            }

            return random.NextRealVectorUniform(minimum ?? searchSpace.Minimum, maximum ?? searchSpace.Maximum, searchSpace.Length);
        }

        public RealVector NextRealVectorNormal(BoundedRealVectorSearchSpace searchSpace, RealVector means, RealVector sigmas) =>
            RealVector.Clamp(random.NextRealVectorNormal(means, sigmas, searchSpace.Length), searchSpace.Minimum, searchSpace.Maximum);
    }
}
