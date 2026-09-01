using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Random;

using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Encodings.RealVectors;

public record AlphaBetaBlendCrossover : SingleCandidateCrossover<RealVector, BoundedRealVectorSearchSpace>, IInvariantContract<RealVector>
{
    /// <summary>
    /// The blend is clamped to the search space bounds, so length and bounds survive at any <see cref="Alpha"/>. A
    /// value outside <c>[0, 1]</c> extrapolates beyond the parents and is pinned to the bounds rather than escaping
    /// them.
    /// </summary>
    public bool? Ensures(ISearchInvariant<RealVector> invariant) => invariant switch
    {
        RealVectorLength or RealVectorBounds => true,
        _ => null
    };

    // Were the final clamp in Cross dropped, so that an extrapolating blend returned its raw result, this operator
    // would keep candidates inside the bounds only while both weights stay in [0, 1]. The contract would then say so
    // rather than the operator being changed to hide it, and validation would report an out-of-range Alpha against
    // the configuration that set it:
    //
    // public bool? Ensures(ISearchInvariant<RealVector> invariant) => invariant switch
    // {
    //     RealVectorLength => true,
    //     RealVectorBounds => Alpha is >= 0.0 and <= 1.0,
    //     _ => null
    // };

    public double Alpha { get; init; } = 0.7;
    public double Beta => 1 - Alpha;

    public override RealVector CrossParents(Parents<RealVector> parents, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace) =>
        Cross(parents.Parent1, parents.Parent2, random, searchSpace, Alpha);

    public static RealVector Cross(RealVector parent1, RealVector parent2, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace, double alpha) =>
        Cross(parent1, parent2, random, alpha, searchSpace.Minimum, searchSpace.Maximum);

    public static RealVector Cross(RealVector parent1, RealVector parent2, IRandomNumberGenerator random, double alpha, RealVector minimum, RealVector maximum)
    {
        var beta = 1 - alpha;
        var result = (alpha * parent1) + (beta * parent2);
        return RealVector.Clamp(result, minimum, maximum);
    }
}
