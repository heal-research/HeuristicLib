namespace HEAL.HeuristicLib.Tests.Operators.Evaluators;

public class ObjectiveVectorAggregatorTests
{
    private static readonly ObjectiveDirections Objective = new(
        [ObjectiveDirection.Minimize],
        Comparer<ObjectiveVector>.Create(static (left, right) => left[0].CompareTo(right[0])));

    [Fact]
    public void Mean_AggregatesEveryObjectiveComponent()
    {
        var objectiveVectors = new[]
        {
            new ObjectiveVector(1.0, 10.0),
            new ObjectiveVector(3.0, 20.0),
            new ObjectiveVector(5.0, 30.0)
        };

        ObjectiveVectorAggregation.Mean.Aggregate(objectiveVectors, Objective)
            .ShouldBe(new ObjectiveVector(3.0, 20.0));
    }

    [Fact]
    public void Mean_RejectsEmptyInput()
    {
        Should.Throw<InvalidOperationException>(() =>
            ObjectiveVectorAggregation.Mean.Aggregate([], Objective));
    }

    [Fact]
    public void DirectionAwareAggregators_UseTotalObjectiveOrder()
    {
        var objectiveVectors = new[]
        {
            new ObjectiveVector(3.0),
            new ObjectiveVector(1.0),
            new ObjectiveVector(2.0)
        };

        ObjectiveVectorAggregation.Median.Aggregate(objectiveVectors, Objective).ShouldBe(new ObjectiveVector(2.0));
        ObjectiveVectorAggregation.Best.Aggregate(objectiveVectors, Objective).ShouldBe(new ObjectiveVector(1.0));
        ObjectiveVectorAggregation.Worst.Aggregate(objectiveVectors, Objective).ShouldBe(new ObjectiveVector(3.0));
    }

    [Fact]
    public void BuiltInAggregators_HaveStructuralValueEquality()
    {
        ObjectiveVectorAggregation.Mean.ShouldBe(new MeanObjectiveVectorAggregator());
        ObjectiveVectorAggregation.Median.ShouldBe(new MedianObjectiveVectorAggregator());
        ObjectiveVectorAggregation.Best.ShouldBe(new BestObjectiveVectorAggregator());
        ObjectiveVectorAggregation.Worst.ShouldBe(new WorstObjectiveVectorAggregator());
    }
}
