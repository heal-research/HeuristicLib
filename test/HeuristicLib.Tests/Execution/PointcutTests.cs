using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Tests.ExecutionInfrastructure;

public class PointcutTests
{
    [Fact]
    public void Reference_DistinguishesStructurallyEqualConfigurations()
    {
        var original = new SizedConfiguration(3);
        var copy = original with { };
        var selector = Pointcut.Reference(original);

        copy.ShouldBe(original);
        selector.Matches(original).ShouldBeTrue();
        selector.Matches(copy).ShouldBeFalse();
    }

    [Fact]
    public void OfType_IncludesDerivedConfigurations()
    {
        var selector = Pointcut.OfType<SizedConfiguration>();

        selector.Matches(new SizedConfiguration(3)).ShouldBeTrue();
        selector.Matches(new DerivedConfiguration(3)).ShouldBeTrue();
        selector.Matches(new OtherConfiguration(3)).ShouldBeFalse();
    }

    [Fact]
    public void OfType_UsesConsumerDefinedInterfaceMembership()
    {
        var selector = Pointcut.OfType<ISizedConfiguration>();

        selector.Matches(new SizedConfiguration(3)).ShouldBeTrue();
        selector.Matches(new AlternateSizedConfiguration(3)).ShouldBeTrue();
        selector.Matches(new OtherConfiguration(3)).ShouldBeFalse();
    }

    [Fact]
    public void AndPredicate_PreservesReferenceIdentity()
    {
        var original = new SizedConfiguration(3);
        Pointcut<SizedConfiguration> selector = Pointcut.Reference(original).And(new SizeRange<SizedConfiguration>(1, int.MaxValue));

        selector.Matches(original).ShouldBeTrue();
        selector.Matches(original with { }).ShouldBeFalse();
    }

    [Fact]
    public void AndPredicate_DoesNotEvaluateAFilterForAnUnmatchedConfiguration()
    {
        var filterCalls = 0;
        var selector = Pointcut.OfType<ISizedConfiguration>()
            .And(new SizeRange<ISizedConfiguration>(1, int.MaxValue))
            .And(new MatchProbe(true, () => filterCalls++));

        selector.Matches(new OtherConfiguration(3)).ShouldBeFalse();
        selector.Matches(new SizedConfiguration(0)).ShouldBeFalse();
        filterCalls.ShouldBe(0);

        selector.Matches(new SizedConfiguration(3)).ShouldBeTrue();
        filterCalls.ShouldBe(1);
    }

    [Fact]
    public void AndPredicate_ComposesFiltersWithoutChangingTheOriginalSelection()
    {
        var original = Pointcut.OfType<ISizedConfiguration>();
        Pointcut<ISizedConfiguration> narrowed = original.And(new SizeRange<ISizedConfiguration>(1, int.MaxValue)).And(new SizeRange<ISizedConfiguration>(int.MinValue, 4));

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
        var left = new Sizes(1, 3);
        var right = new Sizes(3, 5);
        var node = new SizedConfiguration(size);

        left.Or(right).Matches(node).ShouldBe(inUnion);
        (left | right).Matches(node).ShouldBe(inUnion);
        left.And(right).Matches(node).ShouldBe(inIntersection);
        (left & right).Matches(node).ShouldBe(inIntersection);
        left.And(new Sizes(3, 5)).Matches(node).ShouldBe(inIntersection);
    }

    [Fact]
    public void Or_OverlappingBranchesSelectEachSuppliedNodeOnce()
    {
        var preferred = new SizedConfiguration(3);
        var equalCopy = preferred with { };
        var anotherImplementation = new AlternateSizedConfiguration(3);
        var original = Pointcut.Reference<ISizedConfiguration>(preferred);
        var matchingSizes = new Sizes(3);
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
        var left = new MatchProbe(leftMatches, () => evaluated.Add("left"));
        var right = new MatchProbe(true, () => evaluated.Add("right"));

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
        var left = new MatchProbe(leftMatches, () => evaluated.Add("left"));
        var right = new MatchProbe(true, () => evaluated.Add("right"));

        var selected = left & right;
        evaluated.ShouldBeEmpty();

        selected.Matches(new SizedConfiguration(3)).ShouldBe(leftMatches);
        evaluated.ShouldBe(leftMatches ? ["left", "right"] : ["left"]);
    }

    [Fact]
    public void Composition_RejectsAnotherRoleWithoutEvaluatingEitherPredicate()
    {
        var left = new FailingPointcut(new InvalidOperationException("Unexpected left predicate."));
        var right = new FailingPointcut(new InvalidOperationException("Unexpected right predicate."));
        var other = new OtherConfiguration(3);

        left.Or(right).Matches(other).ShouldBeFalse();
        left.And(right).Matches(other).ShouldBeFalse();
    }

    [Fact]
    public void Composition_PropagatesAnEvaluatedPredicateFailure()
    {
        var failure = new InvalidOperationException("Predicate failed.");
        var failing = new FailingPointcut(failure);
        var accepts = Pointcut.OfType<ISizedConfiguration>();
        var rejects = new MatchProbe(false);
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
        var exact = Pointcut.Reference<ISizedConfiguration>(preferred);
        var small = new SizeRange<ISizedConfiguration>(int.MinValue, 4);
        var positive = new SizeRange<ISizedConfiguration>(1, int.MaxValue);

        (exact | small & positive).Matches(preferred).ShouldBeTrue();
        ((exact | small) & positive).Matches(preferred).ShouldBeFalse();
        exact.Or(small).And(positive).Matches(preferred).ShouldBeFalse();
        exact.Matches(preferred).ShouldBeTrue();
        small.Matches(preferred).ShouldBeTrue();
    }

    [Fact]
    public void Predicate_ExaminesOnlyTheDeclaredTypeAndRunsForEachQuery()
    {
        var calls = 0;
        var selected = Pointcut.Predicate<ISizedConfiguration>(configuration =>
        {
            calls++;
            return configuration.Size < 4;
        });
        var node = new SizedConfiguration(3);

        calls.ShouldBe(0);
        selected.Matches(new OtherConfiguration(3)).ShouldBeFalse();
        calls.ShouldBe(0);
        selected.Matches(node).ShouldBeTrue();
        selected.Matches(node).ShouldBeTrue();
        selected.Matches(new AlternateSizedConfiguration(5)).ShouldBeFalse();
        calls.ShouldBe(3);
    }

    [Fact]
    public void Predicate_ComposesWithReferenceMatchingAndPropagatesEvaluatedFailures()
    {
        var node = new SizedConfiguration(3);
        var failure = new InvalidOperationException("Predicate failed.");
        var failing = Pointcut.Predicate<SizedConfiguration>(_ => throw failure);
        var selected = Pointcut.Reference(node).And(failing);

        selected.Matches(node with { }).ShouldBeFalse();
        Should.Throw<InvalidOperationException>(() => selected.Matches(node)).ShouldBeSameAs(failure);
        Should.Throw<InvalidOperationException>(() => (!failing).Matches(node)).ShouldBeSameAs(failure);
        (!failing).Matches(new OtherConfiguration(3)).ShouldBeFalse();
    }

    [Theory]
    [InlineData(3, false)]
    [InlineData(5, true)]
    public void Not_ComplementsOnlyTheDeclaredTypeWithoutChangingItsSource(int size, bool expected)
    {
        var source = Pointcut.Predicate<ISizedConfiguration>(configuration => configuration.Size < 4);
        var node = new SizedConfiguration(size);

        source.Not().Matches(node).ShouldBe(expected);
        (!source).Matches(node).ShouldBe(expected);
        source.Not().Not().Matches(node).ShouldBe(!expected);
        source.Matches(node).ShouldBe(!expected);
        (!source).Matches(new OtherConfiguration(size)).ShouldBeFalse();
    }

    [Theory]
    [InlineData("exploration", true)]
    [InlineData("Exploration", false)]
    [InlineData("exploration ", false)]
    [InlineData("", false)]
    public void Named_UsesExactNamesOnConsumerDefinedConfigurations(string name, bool expected)
    {
        var selected = Pointcut.Named<ISizedConfiguration>("exploration");

        selected.Matches(new NamedSizedConfiguration(name, 3)).ShouldBe(expected);
        selected.Matches(new NamedSizedConfiguration(name, 5)).ShouldBe(expected);
        selected.Matches(new NamedOtherConfiguration(name)).ShouldBeFalse();
        selected.Matches(new SizedConfiguration(3)).ShouldBeFalse();
    }

    [Fact]
    public void Named_ComposesWithPredicateAndNegation()
    {
        var selected = Pointcut.Named<ISizedConfiguration>("exploration") &
            !Pointcut.Predicate<ISizedConfiguration>(configuration => configuration.Size > 4);

        selected.Matches(new NamedSizedConfiguration("exploration", 3)).ShouldBeTrue();
        selected.Matches(new NamedSizedConfiguration("exploration", 5)).ShouldBeFalse();
        selected.Matches(new NamedSizedConfiguration("refinement", 3)).ShouldBeFalse();
    }

    private sealed record NamedSizedConfiguration(string Name, int Size) : ISizedConfiguration, INamedConfiguration;

    private sealed record NamedOtherConfiguration(string Name) : INamedConfiguration;

    private sealed class SizeRange<TConfiguration>(int minimum, int maximum) : Pointcut<TConfiguration>
        where TConfiguration : class, ISizedConfiguration
    {
        public override bool Matches(TConfiguration configuration) => configuration.Size >= minimum && configuration.Size <= maximum;
    }

    private sealed class Sizes(params int[] sizes) : Pointcut<ISizedConfiguration>
    {
        public override bool Matches(ISizedConfiguration configuration) => sizes.Contains(configuration.Size);
    }

    private sealed class MatchProbe(bool result, Action? observed = null) : Pointcut<ISizedConfiguration>
    {
        public override bool Matches(ISizedConfiguration configuration)
        {
            observed?.Invoke();
            return result;
        }
    }

    private sealed class FailingPointcut(Exception failure) : Pointcut<ISizedConfiguration>
    {
        public override bool Matches(ISizedConfiguration configuration) => throw failure;
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
