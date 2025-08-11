using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Genotypes;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.PermutationOperators;

public class OrderCrossover : Crossover<Permutation, PermutationEncoding> 
{
  public override Permutation Cross((Permutation, Permutation) parents, IRandomNumberGenerator random, PermutationEncoding encoding) {
    var (parent1, parent2) = parents;
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
