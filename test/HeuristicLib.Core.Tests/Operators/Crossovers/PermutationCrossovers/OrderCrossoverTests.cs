using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators.Crossovers.PermutationCrossovers;

namespace HEAL.HeuristicLib.Tests;

public class OrderCrossoverTests
{
  [Fact]
  public void Cross_PreservesSelectedSegmentAndFillsRemainingPositionsInParentOrder()
  {
    var parent1 = Permutation.Create(0, 1, 2, 3, 4, 5, 6, 7);
    var parent2 = Permutation.Create(4, 6, 7, 0, 2, 1, 3, 5);

    var result = OrderCrossover.Cross(parent1, parent2, start: 2, end: 4);

    result.ShouldBe(Permutation.Create(7, 0, 2, 3, 4, 1, 5, 6));
  }
}
