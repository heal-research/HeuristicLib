namespace HEAL.HeuristicLib.Tests.Objectives;

public class LexicographicComparerTests
{
    [Theory]
    [InlineData(new int[] { })]
    [InlineData(new[] { 0, 1 })]
    [InlineData(new[] { 0, 1, 2, 3 })]
    [InlineData(new[] { 0, 0, 2 })]
    [InlineData(new[] { -1, 1, 2 })]
    [InlineData(new[] { 0, 1, 3 })]
    public void Construction_RejectsAnOrderThatDoesNotCoverEveryObjectiveOnce(int[] order)
    {
        var exception = Record.Exception(() => new LexicographicComparer(
            [ObjectiveDirection.Minimize, ObjectiveDirection.Minimize, ObjectiveDirection.Minimize], order));

        exception.ShouldNotBeNull();
    }

    [Fact]
    public void CustomOrder_ChangesWhichObjectiveTakesPriority()
    {
        ObjectiveDirection[] directions = [ObjectiveDirection.Minimize, ObjectiveDirection.Minimize];
        var natural = new LexicographicComparer(directions);
        var reversed = new LexicographicComparer(directions, [1, 0]);
        ObjectiveVector first = new[] { 1d, 2d };
        ObjectiveVector second = new[] { 2d, 1d };

        natural.Compare(first, second).ShouldBeLessThan(0);
        reversed.Compare(first, second).ShouldBeGreaterThan(0);
        reversed.Compare(second, first).ShouldBeLessThan(0);
    }

    [Fact]
    public void Ties_AdvanceInCustomOrderAndRespectEachDirection()
    {
        var comparer = new LexicographicComparer(
            [ObjectiveDirection.Minimize, ObjectiveDirection.Maximize, ObjectiveDirection.Minimize], [2, 1, 0]);

        comparer.Compare(new ObjectiveVector(9d, 5d, 1d), new ObjectiveVector(1d, 4d, 1d)).ShouldBeLessThan(0);
        comparer.Compare(new ObjectiveVector(1d, 5d, 1d), new ObjectiveVector(2d, 5d, 1d)).ShouldBeLessThan(0);
        comparer.Compare(new ObjectiveVector(1d, 5d, 1d), new ObjectiveVector(1d, 5d, 1d)).ShouldBe(0);
    }

    [Fact]
    public void Construction_SnapshotsDirectionsAndOrder()
    {
        ObjectiveDirection[] directions = [ObjectiveDirection.Minimize, ObjectiveDirection.Maximize];
        int[] order = [1, 0];
        var comparer = new LexicographicComparer(directions, order);
        directions[1] = ObjectiveDirection.Minimize;
        order[0] = 0;
        order[1] = 1;

        comparer.Compare(new ObjectiveVector(9d, 5d), new ObjectiveVector(1d, 4d)).ShouldBeLessThan(0);
    }

    [Fact]
    public void NoObjectives_AcceptsAnEmptyOrder()
    {
        var comparer = new LexicographicComparer([], []);
        ObjectiveVector empty = Array.Empty<double>();

        comparer.Compare(empty, empty).ShouldBe(0);
    }
}
