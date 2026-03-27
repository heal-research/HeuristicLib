using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.Random.Distributions;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Operators.Mutators.IntegerVectorMutators;

public record RoundedNormalOnePositionManipulator
  : SingleSolutionMutator<IntegerVector, IntegerVectorSearchSpace>
{
  /// <summary>
  /// Standard deviations per dimension (cycled if shorter than the vector).
  /// </summary>
  public RealVector Sigma { get; init; } = new(1.0);

  public override IntegerVector Mutate(IntegerVector parent, IRandomNumberGenerator random, IntegerVectorSearchSpace searchSpace)
    => Manipulate(random, parent, searchSpace, Sigma);

  public static IntegerVector Manipulate(
    IRandomNumberGenerator random,
    IntegerVector vector,
    IntegerVectorSearchSpace searchSpace,
    IReadOnlyList<double> sigma)
  {
    if (sigma is null || sigma.Count == 0)
      throw new ArgumentException("Sigma must be provided and non-empty.", nameof(sigma));

    var length = vector.Count;
    var idx = random.NextInt(0, length); // upper-exclusive

    var s = sigma[idx % sigma.Count];
    if (s < 0)
      throw new ArgumentOutOfRangeException(nameof(sigma), "All sigma values must be >= 0.");

    var res = vector.ToArray();

    var value = random.NextGaussian(res[idx], s);
    res[idx] = searchSpace.RoundFeasible(value, idx);

    return res;
  }
}
