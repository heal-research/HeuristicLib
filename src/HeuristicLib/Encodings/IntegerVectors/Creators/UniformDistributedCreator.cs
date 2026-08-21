using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Encodings.IntegerVectors;

public record UniformDistributedCreator : SingleCandidateCreator<IntegerVector, IntegerVectorSearchSpace>
{
    public override IntegerVector CreateCandidate(IRandomNumberGenerator random, IntegerVectorSearchSpace searchSpace) =>
        random.NextIntegerVectorUniform(searchSpace);

    public static IntegerVector Create(IntegerVectorSearchSpace searchSpace, IRandomNumberGenerator random) =>
        random.NextIntegerVectorUniform(searchSpace);

    public static IntegerVector Create(IRandomNumberGenerator random, int length, IntegerVector minimum, IntegerVector maximum) =>
        random.NextIntegerVectorUniform(minimum, maximum, length);

    public static IntegerVector Create(IRandomNumberGenerator random, int length, int minimum, int maximum) =>
        Create(random, length, [minimum], [maximum]);
}
