using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Encodings.IntegerVectors;

public record NormalDistributedCreator(RealVector Means, RealVector Sigmas)
    : SingleCandidateCreator<IntegerVector, IntegerVectorSearchSpace>
{
    public override IntegerVector CreateCandidate(IRandomNumberGenerator random, IntegerVectorSearchSpace searchSpace) =>
        Create(random, searchSpace, Means, Sigmas);

    public static IntegerVector Create(IRandomNumberGenerator random, IntegerVectorSearchSpace searchSpace, RealVector means, RealVector sigmas) =>
        random.NextIntegerVectorNormal(searchSpace, means, sigmas);

    public static IntegerVector Create(IRandomNumberGenerator random, int length, RealVector means, RealVector sigmas, IntegerVector minimum, IntegerVector maximum) =>
        random.NextIntegerVectorNormal(means, sigmas, minimum, maximum, length);
}
