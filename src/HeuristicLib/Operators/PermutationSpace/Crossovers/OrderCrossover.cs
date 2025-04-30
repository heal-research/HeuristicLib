using HEAL.HeuristicLib.Genotypes;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.PermutationSpace;

public record class OrderCrossover : Crossover<Permutation, PermutationSearchSpace> {
  public override OrderCrossoverInstance CreateInstance() => new OrderCrossoverInstance(this);
}

public class OrderCrossoverInstance : CrossoverInstance<Permutation, PermutationSearchSpace, OrderCrossover> {
  public OrderCrossoverInstance(OrderCrossover parameters) : base(parameters) { }
  public override Permutation Cross(Permutation parent1, Permutation parent2, PermutationSearchSpace searchSpace, IRandomNumberGenerator random) {
    return Permutation.OrderCrossover(parent1, parent2, random);
  }

  // public record BreakPoints(int First, int Second) {
  //   public static BreakPoints SingleRandom(int length, IRandomNumberGenerator rng) {
  //     int first = rng.Integer(1, length - 1);
  //     int second = rng.Integer(first + 1, length);
  //     return new BreakPoints(first, second);
  //   }
  //   public static IEnumerable<BreakPoints> MultipleRandom(int length, int count, IRandomNumberGenerator rng) {
  //     int maxPossiblePairs = length * (length - 1) / 2;
  //     count = Math.Min(count, maxPossiblePairs);
  //     var chosenBreakPoints = new HashSet<BreakPoints>();
  //     while (chosenBreakPoints.Count < count) {
  //       var breakPoints = SingleRandom(length, rng);
  //       if (chosenBreakPoints.Add(breakPoints)) {
  //         yield return breakPoints;
  //       }
  //     }
  //   }
  //   public static IEnumerable<BreakPoints> Exhaustive(int length) {
  //     for (int first = 1; first < length - 1; first++) {
  //       for (int second = first + 1; second < length; second++) {
  //         yield return new BreakPoints(first, second);
  //       }
  //     }
  //   }
  // }
}
