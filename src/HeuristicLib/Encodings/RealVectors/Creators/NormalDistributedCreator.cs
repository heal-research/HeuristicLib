using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Encodings.RealVectors;

public record NormalDistributedCreator(RealVector Means, RealVector Sigmas)
    : SingleCandidateCreator<RealVector, RealVectorSearchSpace>
{
    public override RealVector CreateCandidate(IRandomNumberGenerator random, RealVectorSearchSpace searchSpace) =>
        Create(random, searchSpace, Means, Sigmas);

    public static RealVector Create(IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, RealVector means, RealVector sigmas) =>
        random.NextRealVectorNormal(searchSpace, means, sigmas);

    public static RealVector Create(IRandomNumberGenerator random, int length, RealVector means, RealVector sigmas, RealVector minimum, RealVector maximum) =>
        RealVector.Clamp(random.NextRealVectorNormal(means, sigmas, length), minimum, maximum);
}
