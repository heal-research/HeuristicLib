using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Tests.Optimization;

public class ObjectiveDirectionsTests
{
    [Fact]
    public void Constructor_SnapshotsDirections()
    {
        var directions = new List<ObjectiveDirection> { ObjectiveDirection.Minimize };
        var objectives = new ObjectiveDirections(directions, NoTotalOrderComparer.Instance);

        directions[0] = ObjectiveDirection.Maximize;

        objectives.Directions.ShouldBe([ObjectiveDirection.Minimize]);
    }

    [Fact]
    public void ObjectiveValue_UnknownDirection_ThrowsInvalidOperationException()
    {
        var direction = (ObjectiveDirection)int.MaxValue;
        var value = new ObjectiveValue(1.0);

        Should.Throw<InvalidOperationException>(() => value.CompareTo(value, direction));
        Should.Throw<InvalidOperationException>(() => ObjectiveValue.BestValue(direction));
        Should.Throw<InvalidOperationException>(() => ObjectiveValue.WorstValue(direction));
    }

    [Fact]
    public void ObjectiveVector_UnknownDirection_ThrowsInvalidOperationException()
    {
        var direction = (ObjectiveDirection)int.MaxValue;
        var objective = new ObjectiveDirections([direction], NoTotalOrderComparer.Instance);

        Should.Throw<InvalidOperationException>(() =>
            new ObjectiveVector(1.0).CompareTo(new ObjectiveVector(2.0), objective));
    }

    [Fact]
    public void ObjectiveVectorSequenceExtensions_UseTotalOrderComparer()
    {
        var objective = new ObjectiveDirections(
            [ObjectiveDirection.Minimize],
            new LexicographicComparer([ObjectiveDirection.Minimize]));
        ObjectiveVector[] values = [new(4.0), new(1.0), new(3.0), new(2.0)];

        values.Best(objective).ShouldBe(new ObjectiveVector(1.0));
        values.Worst(objective).ShouldBe(new ObjectiveVector(4.0));
        values.Median(objective).ShouldBe(new ObjectiveVector(3.0));
        values.Mean().ShouldBe(new ObjectiveVector(2.5));
    }

    [Fact]
    public void ObjectiveVectorSequenceExtensions_EmptySequence_Throw()
    {
        var objective = new ObjectiveDirections(
            [ObjectiveDirection.Minimize],
            new LexicographicComparer([ObjectiveDirection.Minimize]));
        var values = Array.Empty<ObjectiveVector>();

        Should.Throw<InvalidOperationException>(() => values.Best(objective));
        Should.Throw<InvalidOperationException>(() => values.Worst(objective));
        Should.Throw<InvalidOperationException>(() => values.Median(objective));
        Should.Throw<InvalidOperationException>(() => values.Mean());
    }
}
