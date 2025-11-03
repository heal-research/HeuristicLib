using HEAL.HeuristicLib.Operators.BatchOperators;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

public class RandomCrossover<TGenotype> : BatchCrossover<TGenotype> {
  public double Bias { get; }

  public RandomCrossover(double bias = 0.5) {
    if (bias is < 0 or > 1)
      throw new ArgumentOutOfRangeException(nameof(bias), "Bias must be between 0 and 1.");
    Bias = bias;
  }

  public override IReadOnlyList<TGenotype> Cross(IReadOnlyList<(TGenotype, TGenotype)> parents, IRandomNumberGenerator random) {
    var randoms = random.Random(parents.Count);
    var offspring = new TGenotype[parents.Count];

    Parallel.For(0, parents.Count, i => {
      var (parent1, parent2) = parents[i];
      offspring[i] = randoms[i] < Bias ? parent1 : parent2;
    });

    return offspring;
  }
}
