using HEAL.HeuristicLib.Operators;
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
    => Mutate(random, parent, searchSpace, Sigma);

  public static IntegerVector Mutate(
    IRandomNumberGenerator random,
    IntegerVector vector,
    IntegerVectorSearchSpace searchSpace,
    IReadOnlyList<double> sigma)
    => Mutate(random, vector, searchSpace.Minimum, searchSpace.Maximum, sigma);

  public static IntegerVector Mutate(
    IRandomNumberGenerator random,
    IntegerVector vector,
    IntegerVector minimum,
    IntegerVector maximum,
    IReadOnlyList<double> sigma)
  {
    if (sigma is null || sigma.Count == 0)
      throw new ArgumentException("Sigma must be provided and non-empty.", nameof(sigma));

    var length = vector.Count;
    var idx = random.NextInt(0, length); // upper-exclusive

    var s = sigma[idx % sigma.Count];
    if (s < 0)
      throw new ArgumentOutOfRangeException(nameof(sigma), "All sigma values must be >= 0.");

    var result = vector.ToArray();
    var value = random.NextGaussian(vector[idx], s);
    result[idx] = RealVector.RoundToIntegerAt(value, minimum, maximum, idx);

    return result;
  }
}
