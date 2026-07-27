using HEAL.HeuristicLib.Collections;

namespace HEAL.HeuristicLib.Tests.Collections;

public class EnumerableExtensionsTests
{
    [Fact]
    public void SelectCircularPairs_EmptySequence_ReturnsEmpty()
    {
        var result = Array.Empty<int>().SelectCircularPairs((left, right) => (left, right));

        result.ShouldBeEmpty();
    }

    [Fact]
    public void SelectCircularPairs_SingleItem_ReturnsEmpty()
    {
        var result = new[] { 1 }.SelectCircularPairs((left, right) => (left, right));

        result.ShouldBeEmpty();
    }

    [Fact]
    public void SelectCircularPairs_SelectsAdjacentAndClosingPairs()
    {
        var result = new[] { 1, 2, 3 }
            .SelectCircularPairs((left, right) => (left, right))
            .ToArray();

        result.ShouldBe([(1, 2), (2, 3), (3, 1)]);
    }
}
