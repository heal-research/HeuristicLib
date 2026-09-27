using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Tests.ExecutionInfrastructure;

public class NodeSelectorTests
{
    [Fact]
    public void Reference_DistinguishesStructurallyEqualConfigurations()
    {
        var original = new SizedConfiguration(3);
        var copy = original with { };
        var selector = NodeSelector.Reference(original);

        copy.ShouldBe(original);
        selector.Matches(original).ShouldBeTrue();
        selector.Matches(copy).ShouldBeFalse();
    }

    [Fact]
    public void OfType_IncludesDerivedConfigurations()
    {
        var selector = NodeSelector.OfType<SizedConfiguration>();

        selector.Matches(new SizedConfiguration(3)).ShouldBeTrue();
        selector.Matches(new DerivedConfiguration(3)).ShouldBeTrue();
        selector.Matches(new OtherConfiguration(3)).ShouldBeFalse();
    }

    [Fact]
    public void OfType_UsesConsumerDefinedInterfaceMembership()
    {
        var selector = NodeSelector.OfType<ISizedConfiguration>();

        selector.Matches(new SizedConfiguration(3)).ShouldBeTrue();
        selector.Matches(new AlternateSizedConfiguration(3)).ShouldBeTrue();
        selector.Matches(new OtherConfiguration(3)).ShouldBeFalse();
    }

    [Fact]
    public void AndPredicate_PreservesReferenceIdentity()
    {
        var original = new SizedConfiguration(3);
        NodeSelector<SizedConfiguration> selector = NodeSelector.Reference(original).And(node => node.Size > 0);

        selector.Matches(original).ShouldBeTrue();
        selector.Matches(original with { }).ShouldBeFalse();
    }

    [Fact]
    public void AndPredicate_DoesNotEvaluateAFilterForAnUnmatchedConfiguration()
    {
        var filterCalls = 0;
        var selector = NodeSelector.OfType<ISizedConfiguration>()
            .And(node => node.Size > 0)
            .And(_ =>
            {
                filterCalls++;
                return true;
            });

        selector.Matches(new OtherConfiguration(3)).ShouldBeFalse();
        selector.Matches(new SizedConfiguration(0)).ShouldBeFalse();
        filterCalls.ShouldBe(0);

        selector.Matches(new SizedConfiguration(3)).ShouldBeTrue();
        filterCalls.ShouldBe(1);
    }

    [Fact]
    public void AndPredicate_ComposesFiltersWithoutChangingTheOriginalSelection()
    {
        var original = NodeSelector.OfType<ISizedConfiguration>();
        NodeSelector<ISizedConfiguration> narrowed = original.And(node => node.Size > 0).And(node => node.Size < 5);

        narrowed.Matches(new SizedConfiguration(0)).ShouldBeFalse();
        narrowed.Matches(new SizedConfiguration(3)).ShouldBeTrue();
        narrowed.Matches(new SizedConfiguration(5)).ShouldBeFalse();
        original.Matches(new SizedConfiguration(0)).ShouldBeTrue();
        original.Matches(new SizedConfiguration(5)).ShouldBeTrue();
    }

    [Theory]
    [InlineData(1, true, false)]
    [InlineData(3, true, true)]
    [InlineData(5, true, false)]
    [InlineData(7, false, false)]
    public void Composition_MatchesTheUnionAndIntersection(int size, bool inUnion, bool inIntersection)
    {
        var left = new NodeSelector<ISizedConfiguration>(node => node.Size is 1 or 3);
        var right = new NodeSelector<ISizedConfiguration>(node => node.Size is 3 or 5);
        var node = new SizedConfiguration(size);

        left.Or(right).Matches(node).ShouldBe(inUnion);
        (left | right).Matches(node).ShouldBe(inUnion);
        left.And(right).Matches(node).ShouldBe(inIntersection);
        (left & right).Matches(node).ShouldBe(inIntersection);
        left.And(candidate => candidate.Size is 3 or 5).Matches(node).ShouldBe(inIntersection);
    }

    [Fact]
    public void Or_OverlappingBranchesSelectEachSuppliedNodeOnce()
    {
        var preferred = new SizedConfiguration(3);
        var equalCopy = preferred with { };
        var anotherImplementation = new AlternateSizedConfiguration(3);
        var original = NodeSelector.Reference<ISizedConfiguration>(preferred);
        var matchingSizes = new NodeSelector<ISizedConfiguration>(node => node.Size == 3);
        var selected = original.Or(matchingSizes);
        IConfigurationNode[] nodes = [preferred, equalCopy, anotherImplementation, new SizedConfiguration(5)];

        var matches = nodes.Where(selected.Matches).ToArray();

        matches.Length.ShouldBe(3);
        matches[0].ShouldBeSameAs(preferred);
        matches[1].ShouldBeSameAs(equalCopy);
        matches[2].ShouldBeSameAs(anotherImplementation);
        original.Matches(equalCopy).ShouldBeFalse();
        original.Matches(anotherImplementation).ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Or_EvaluatesPredicatesOnlyWhenMatchingAndStopsAfterASuccess(bool leftMatches)
    {
        var evaluated = new List<string>();
        var left = new NodeSelector<ISizedConfiguration>(_ =>
        {
            evaluated.Add("left");
            return leftMatches;
        });
        var right = new NodeSelector<ISizedConfiguration>(_ =>
        {
            evaluated.Add("right");
            return true;
        });

        var selected = left | right;
        evaluated.ShouldBeEmpty();

        selected.Matches(new SizedConfiguration(3)).ShouldBeTrue();
        evaluated.ShouldBe(leftMatches ? ["left"] : ["left", "right"]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void And_EvaluatesPredicatesOnlyWhenMatchingAndStopsAfterAFailure(bool leftMatches)
    {
        var evaluated = new List<string>();
        var left = new NodeSelector<ISizedConfiguration>(_ =>
        {
            evaluated.Add("left");
            return leftMatches;
        });
        var right = new NodeSelector<ISizedConfiguration>(_ =>
        {
            evaluated.Add("right");
            return true;
        });

        var selected = left & right;
        evaluated.ShouldBeEmpty();

        selected.Matches(new SizedConfiguration(3)).ShouldBe(leftMatches);
        evaluated.ShouldBe(leftMatches ? ["left", "right"] : ["left"]);
    }

    [Fact]
    public void Composition_RejectsAnotherRoleWithoutEvaluatingEitherPredicate()
    {
        var left = new NodeSelector<ISizedConfiguration>(_ => throw new InvalidOperationException("Unexpected left predicate."));
        var right = new NodeSelector<ISizedConfiguration>(_ => throw new InvalidOperationException("Unexpected right predicate."));
        var other = new OtherConfiguration(3);

        left.Or(right).Matches(other).ShouldBeFalse();
        left.And(right).Matches(other).ShouldBeFalse();
    }

    [Fact]
    public void Composition_PropagatesAnEvaluatedPredicateFailure()
    {
        var failure = new InvalidOperationException("Predicate failed.");
        var failing = new NodeSelector<ISizedConfiguration>(_ => throw failure);
        var accepts = NodeSelector.OfType<ISizedConfiguration>();
        var rejects = new NodeSelector<ISizedConfiguration>(_ => false);
        var node = new SizedConfiguration(3);

        Should.Throw<InvalidOperationException>(() => accepts.And(failing).Matches(node)).ShouldBeSameAs(failure);
        Should.Throw<InvalidOperationException>(() => rejects.Or(failing).Matches(node)).ShouldBeSameAs(failure);
        accepts.Or(failing).Matches(node).ShouldBeTrue();
        rejects.And(failing).Matches(node).ShouldBeFalse();
    }

    [Fact]
    public void AFilterAfterUnionAppliesToBothBranches()
    {
        var preferred = new SizedConfiguration(0);
        var exact = NodeSelector.Reference<ISizedConfiguration>(preferred);
        var small = new NodeSelector<ISizedConfiguration>(node => node.Size < 5);
        var positive = new NodeSelector<ISizedConfiguration>(node => node.Size > 0);

        (exact | small & positive).Matches(preferred).ShouldBeTrue();
        ((exact | small) & positive).Matches(preferred).ShouldBeFalse();
        exact.Or(small).And(positive).Matches(preferred).ShouldBeFalse();
        exact.Matches(preferred).ShouldBeTrue();
        small.Matches(preferred).ShouldBeTrue();
    }

    private interface ISizedConfiguration : IConfigurationNode
    {
        int Size { get; }
    }

    private record SizedConfiguration(int Size) : ISizedConfiguration;

    private sealed record DerivedConfiguration(int Size) : SizedConfiguration(Size);

    private sealed record AlternateSizedConfiguration(int Size) : ISizedConfiguration;

    private sealed record OtherConfiguration(int Size) : IConfigurationNode;
}
