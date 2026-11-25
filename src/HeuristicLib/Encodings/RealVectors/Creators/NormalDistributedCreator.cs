using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Encodings.RealVectors;

public record NormalDistributedCreator(RealVector Means, RealVector Sigmas)
    : SingleCandidateCreator<RealVector, BoundedRealVectorSearchSpace>
{
    public static Parameter<NormalDistributedCreator, RealVector> MeansParameter { get; } =
        new(nameof(Means), static creator => creator.Means, static (creator, value) => creator with { Means = value });

    public static Parameter<NormalDistributedCreator, RealVector> SigmasParameter { get; } =
        new(nameof(Sigmas), static creator => creator.Sigmas, static (creator, value) => creator with { Sigmas = value });

    public new static IReadOnlyList<IParameter> Parameters { get; } = [MeansParameter, SigmasParameter];

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

    public override RealVector CreateCandidate(IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace) =>
        Create(random, searchSpace, Means, Sigmas);

    public static RealVector Create(IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace, RealVector means, RealVector sigmas) =>
        random.NextRealVectorNormal(searchSpace, means, sigmas);

    public static RealVector Create(IRandomNumberGenerator random, int length, RealVector means, RealVector sigmas, RealVector minimum, RealVector maximum) =>
        RealVector.Clamp(random.NextRealVectorNormal(means, sigmas, length), minimum, maximum);
}
