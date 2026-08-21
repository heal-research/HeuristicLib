using HEAL.HeuristicLib.Encodings.Permutations;

namespace HEAL.HeuristicLib.Tests.Operators.Crossovers.PermutationCrossovers;

public class OrderCrossoverTests
{
    public static IEnumerable<object[]> ReferenceExamples()
    {
        yield return [
          Permutation.Create(0, 1, 2, 3, 4, 5, 6, 7, 8),
      Permutation.Create(8, 2, 6, 7, 1, 5, 4, 0, 3),
      3,
      6,
      Permutation.Create(2, 7, 1, 3, 4, 5, 6, 0, 8)
        ];
        yield return [
          Permutation.Create(0, 1, 2, 3, 4, 5, 6, 7),
      Permutation.Create(1, 3, 5, 7, 6, 4, 2, 0),
      2,
      4,
      Permutation.Create(7, 6, 2, 3, 4, 0, 1, 5)
        ];
        yield return [
          Permutation.Create(0, 1, 2, 3, 4, 5, 6, 7, 8),
      Permutation.Create(7, 3, 0, 4, 8, 2, 5, 1, 6),
      2,
      5,
      Permutation.Create(0, 8, 2, 3, 4, 5, 1, 6, 7)
        ];
        yield return [
          Permutation.Create(2, 1, 4, 3, 7, 8, 6, 0, 5, 9),
      Permutation.Create(5, 3, 4, 0, 9, 8, 2, 7, 1, 6),
      0,
      5,
      Permutation.Create(2, 1, 4, 3, 7, 8, 6, 5, 0, 9)
        ];
        yield return [
          Permutation.Create(2, 1, 4, 3, 7, 8, 6, 0, 5, 9),
      Permutation.Create(5, 3, 4, 0, 9, 8, 2, 7, 1, 6),
      6,
      9,
      Permutation.Create(3, 4, 8, 2, 7, 1, 6, 0, 5, 9)
        ];
        yield return [
          Permutation.Create(2, 1, 4, 3, 7, 8, 6, 0, 5, 9),
      Permutation.Create(5, 3, 4, 0, 9, 8, 2, 7, 1, 6),
      0,
      9,
      Permutation.Create(2, 1, 4, 3, 7, 8, 6, 0, 5, 9)
        ];
    }

    [Theory]
    [MemberData(nameof(ReferenceExamples))]
    public void Cross_MatchesReferenceExamples(
      Permutation parent1,
      Permutation parent2,
      int start,
      int end,
      Permutation expected)
    {
        var result = OrderCrossover.Cross(parent1, parent2, start, end);

        result.ShouldBe(expected);
    }

    [Fact]
    public void Cross_PreservesSelectedSegmentAndFillsRemainingPositionsInParentOrder()
    {
        var parent1 = Permutation.Create(0, 1, 2, 3, 4, 5, 6, 7);
        var parent2 = Permutation.Create(4, 6, 7, 0, 2, 1, 3, 5);

        var result = OrderCrossover.Cross(parent1, parent2, start: 2, end: 4);

        result.ShouldBe(Permutation.Create(7, 0, 2, 3, 4, 1, 5, 6));
    }

    [Fact]
    public void Cross_ThrowsForParentsWithDifferentLengths()
    {
        var parent1 = Permutation.Range(8);
        var parent2 = Permutation.Range(6);

        Should.Throw<ArgumentException>(() => OrderCrossover.Cross(parent1, parent2, start: 2, end: 4));
    }
}
