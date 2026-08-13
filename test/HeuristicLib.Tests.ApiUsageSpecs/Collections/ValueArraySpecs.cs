namespace HEAL.HeuristicLib.Tests.ApiUsageSpecs.Collections;

/// <summary>
/// Documents how a configuration type stores an ordered collection so that structural equality follows from the
/// member type alone, without an equality attribute, a source generator, or a hand-written comparison.
/// </summary>
public class ValueArraySpecs
{
    private sealed record Stage(string Name);

    private sealed record Workflow
    {
        public Workflow(params IReadOnlyList<Stage> stages) => Stages = stages.ToValueArray();

        public ValueArray<Stage> Stages { get; }
    }

    [Fact]
    public void ConfigurationAcceptsAnyReadOnlyListAndComparesItsElements()
    {
        var fromParameters = new Workflow(new Stage("normalize"), new Stage("optimize"));
        var fromCollectionExpression = new Workflow([new Stage("normalize"), new Stage("optimize")]);
        var fromMutableList = new Workflow(new List<Stage> { new("normalize"), new("optimize") });

        fromParameters.ShouldBe(fromCollectionExpression);
        fromParameters.ShouldBe(fromMutableList);
        fromParameters.GetHashCode().ShouldBe(fromMutableList.GetHashCode());
    }

    [Fact]
    public void ConfigurationSnapshotsTheCallerCollection()
    {
        var stages = new List<Stage> { new("normalize") };
        var workflow = new Workflow(stages);

        stages.Add(new Stage("optimize"));

        workflow.Stages.Count.ShouldBe(1);
    }

    [Fact]
    public void OrderIsPartOfTheConfigurationIdentity()
    {
        var forward = new Workflow(new Stage("normalize"), new Stage("optimize"));
        var reversed = new Workflow(new Stage("optimize"), new Stage("normalize"));

        forward.ShouldNotBe(reversed);
    }

    [Fact]
    public void CollectionsAreBuiltFromElementsOrSnapshotFromSequences()
    {
        var fromElements = ValueArray.Create(1, 2, 3);
        ValueArray<int> fromCollectionExpression = [1, 2, 3];
        var fromSequence = Enumerable.Range(1, 3).ToValueArray();
        var extended = ValueArray<int>.Empty;

        extended = [.. extended, .. fromElements];

        fromElements.ShouldBe(fromCollectionExpression);
        fromElements.ShouldBe(fromSequence);
        extended.ShouldBe(fromElements);
    }

    [Fact]
    public void AnUnassignedCollectionMemberIsAnEmptyCollection()
    {
        var workflow = new Workflow();

        workflow.Stages.Count.ShouldBe(0);
        workflow.Stages.ShouldBe(ValueArray<Stage>.Empty);
    }
}
