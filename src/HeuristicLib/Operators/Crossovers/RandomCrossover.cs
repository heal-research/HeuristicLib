using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Crossovers;

public class RandomCrossover<TGenotype> : BatchCrossover<TGenotype> {
  public double Bias { get; }

  public RandomCrossover(double bias = 0.5) {
    if (bias is < 0 or > 1)
      throw new ArgumentOutOfRangeException(nameof(bias), "Bias must be between 0 and 1.");
    Bias = bias;
  }

  public override IReadOnlyList<TGenotype> Cross(IReadOnlyList<IParents<TGenotype>> parents, IRandomNumberGenerator random) {
    return parents.ParallelSelect(random, (_, parents1, random) => random.Random() < Bias ? parents1.Parent1 : parents1.Parent2);
  }
}
