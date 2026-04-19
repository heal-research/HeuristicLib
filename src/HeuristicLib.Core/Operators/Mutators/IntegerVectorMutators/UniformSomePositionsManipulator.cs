using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Operators.Mutators.IntegerVectorMutators;

/// <summary>
/// Mutates each position independently with probability <see cref="Probability"/> by
/// replacing it with a uniformly sampled feasible value from the search space bounds.
/// </summary>
public record UniformSomePositionsManipulator
  : SingleSolutionMutator<IntegerVector, IntegerVectorSearchSpace>
{
  public double Probability
  {
    get;
    init {
      ArgumentOutOfRangeException.ThrowIfLessThan(value, 0);
      ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 1);
      field = value;
    }
  } = 0.05;

  public bool AtLeastOnce { get; init; } = true;

  public override IntegerVector Mutate(IntegerVector parent, IRandomNumberGenerator random, IntegerVectorSearchSpace searchSpace)
    => Mutate(random, parent, searchSpace, Probability, AtLeastOnce);

  public static IntegerVector Mutate(
    IRandomNumberGenerator random,
    IntegerVector vector,
    IntegerVectorSearchSpace searchSpace,
    double probability, bool atLeastOnce = true)
    => Mutate(random, vector, searchSpace.Minimum, searchSpace.Maximum, probability, atLeastOnce);

  public static IntegerVector Mutate(
    IRandomNumberGenerator random,
    IntegerVector vector,
    IntegerVector minimum,
    IntegerVector maximum,
    double probability,
    bool atLeastOnce = true)
  {
    if (probability < 0 || probability > 1)
      throw new ArgumentOutOfRangeException(nameof(probability), "probability must be in [0,1].");

    var res = vector.ToArray();
    var mutated = false;
    var length = vector.Count;
    for (var i = 0; i < length; i++) {
      if (random.NextDouble() >= probability)
        continue;
      mutated = true;
      res[i] = random.NextInt(minimum, maximum, i);
    }

    if (!mutated && atLeastOnce) {
      var idx = random.NextInt(0, vector.Count);
      res[idx] = random.NextInt(minimum, maximum, idx);
    }

    return res;
  }
}
