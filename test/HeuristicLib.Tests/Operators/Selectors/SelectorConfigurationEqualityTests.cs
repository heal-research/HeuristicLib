using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Operators.Selectors;

/// <summary>
/// Pins the child slot of the selector topology bases: the configured children are publicly inspectable and the
/// child collection compares by ordered structural equality rather than by array reference.
/// </summary>
public class SelectorConfigurationEqualityTests
{
    [Fact]
    public void WrappingSelector_ExposesConfiguredChildSelector()
    {
        var childSelector = new RangeSelector(1);

        var selector = childSelector.AvoidSameMates(maximumAttempts: 3);

        selector.ChildSelector.ShouldBeSameAs(childSelector);
    }

    [Fact]
    public void MultiSelector_ExposesConfiguredChildSelectors()
    {
        var first = new RangeSelector(1);
        var second = new RangeSelector(2);

        var selector = ChooseOneSelector.Create(first, second);

        selector.ChildSelectors.ShouldBe([first, second]);
    }

    [Fact]
    public void MultiSelector_SnapshotsConfiguredChildSelectors()
    {
        var first = new RangeSelector(1);
        var second = new RangeSelector(2);
        var childSelectors = new List<ISelector<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>> { first, second };
        var selector = new FirstOfSelector(childSelectors);

        childSelectors.Clear();

        selector.ChildSelectors.ShouldBe([first, second]);
    }

    [Fact]
    public void MultiSelector_WithSeparatelyBuiltEqualChildCollections_IsEqual()
    {
        var left = ChooseOneSelector.Create(new RangeSelector(1), new RangeSelector(2));
        var right = ChooseOneSelector.Create(new RangeSelector(1), new RangeSelector(2));

        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void MultiSelector_WithDifferentChildSelectors_IsNotEqual()
    {
        var left = ChooseOneSelector.Create(new RangeSelector(1), new RangeSelector(2));
        var right = ChooseOneSelector.Create(new RangeSelector(1), new RangeSelector(3));

        left.ShouldNotBe(right);
    }

    [Fact]
    public void MultiSelector_WithReorderedChildSelectors_IsNotEqual()
    {
        var left = ChooseOneSelector.Create(new RangeSelector(1), new RangeSelector(2));
        var right = ChooseOneSelector.Create(new RangeSelector(2), new RangeSelector(1));

        left.ShouldNotBe(right);
    }

    [Fact]
    public void MultiSelector_WithDifferentChildCount_IsNotEqual()
    {
        var left = ChooseOneSelector.Create(new RangeSelector(1), new RangeSelector(2));
        var right = ChooseOneSelector.Create(new RangeSelector(1));

        left.ShouldNotBe(right);
    }

    [Fact]
    public void ChooseOneSelector_WithDifferentWeights_IsNotEqual()
    {
        ISelector<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>[] childSelectors = [new RangeSelector(1), new RangeSelector(2)];
        var left = new ChooseOneSelector<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(childSelectors) { Weights = [1.0, 2.0] };
        var right = new ChooseOneSelector<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(childSelectors) { Weights = [2.0, 1.0] };

        left.ShouldNotBe(right);
    }

    [Fact]
    public void MultiSelectorTopologies_WithEqualChildSelectors_AreNotEqualAcrossTypes()
    {
        var chooseOne = ChooseOneSelector.Create(new RangeSelector(1), new RangeSelector(2));
        var firstOf = new FirstOfSelector([new RangeSelector(1), new RangeSelector(2)]);

        chooseOne.ShouldNotBe<object>(firstOf);
    }

    /// <summary>
    /// Operators whose children carry a specific role declare those children themselves instead of deriving from a
    /// multi base, so their equality comes from the named properties rather than from an ordered child collection.
    /// </summary>
    [Fact]
    public void RoleBearingChildren_WithEqualChildSelectors_IsEqual()
    {
        var left = GenderSpecificSelector.Create(new RangeSelector(1), new RangeSelector(2));
        var right = GenderSpecificSelector.Create(new RangeSelector(1), new RangeSelector(2));

        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void RoleBearingChildren_WithSwappedChildSelectors_IsNotEqual()
    {
        var left = GenderSpecificSelector.Create(new RangeSelector(1), new RangeSelector(2));
        var right = GenderSpecificSelector.Create(new RangeSelector(2), new RangeSelector(1));

        left.ShouldNotBe(right);
    }

    [Fact]
    public void WrappingSelector_WithEqualChildSelector_IsEqual()
    {
        var left = new RangeSelector(1).AvoidSameMates(maximumAttempts: 3);
        var right = new RangeSelector(1).AvoidSameMates(maximumAttempts: 3);

        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void WrappingSelector_WithDifferentChildSelector_IsNotEqual()
    {
        var left = new RangeSelector(1).AvoidSameMates(maximumAttempts: 3);
        var right = new RangeSelector(2).AvoidSameMates(maximumAttempts: 3);

        left.ShouldNotBe(right);
    }

    [Fact]
    public void EliteSelector_WithDifferentChildSelector_IsNotEqual()
    {
        var left = new RangeSelector(1).WithElites(2);
        var right = new RangeSelector(2).WithElites(2);

        left.ShouldNotBe(right);
    }

    [Fact]
    public void CountingSelector_WithSameCounterAndMetric_IsEqual()
    {
        var counter = new ObservationCounter();
        var left = new RangeSelector(1).CountSelectorCalls(counter);
        var right = new RangeSelector(1).CountSelectorCalls(counter);

        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void CountingSelector_WithDifferentMetric_IsNotEqual()
    {
        var counter = new ObservationCounter();
        var left = new RangeSelector(1).CountSelectorCalls(counter);
        var right = new RangeSelector(1).CountSelectedCandidates(counter);

        left.ShouldNotBe(right);
    }

    [Fact]
    public void CountingSelector_WithDifferentCounter_IsNotEqual()
    {
        var left = new RangeSelector(1).CountSelectorCalls(new ObservationCounter());
        var right = new RangeSelector(1).CountSelectorCalls(new ObservationCounter());

        left.ShouldNotBe(right);
    }

    [Fact]
    public void CountingSelector_WithDifferentChildSelector_IsNotEqual()
    {
        var counter = new ObservationCounter();
        var left = new RangeSelector(1).CountSelectorCalls(counter);
        var right = new RangeSelector(2).CountSelectorCalls(counter);

        left.ShouldNotBe(right);
    }

    [Fact]
    public void DurationMeasuringSelector_WithSameDurationAndTimeProvider_IsEqual()
    {
        var duration = new ObservationDuration();
        var left = new RangeSelector(1).MeasureSelectorDuration(duration, TimeProvider.System);
        var right = new RangeSelector(1).MeasureSelectorDuration(duration, TimeProvider.System);

        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void DurationMeasuringSelector_WithDifferentDuration_IsNotEqual()
    {
        var left = new RangeSelector(1).MeasureSelectorDuration(new ObservationDuration(), TimeProvider.System);
        var right = new RangeSelector(1).MeasureSelectorDuration(new ObservationDuration(), TimeProvider.System);

        left.ShouldNotBe(right);
    }

    [Fact]
    public void DurationMeasuringSelector_WithDifferentChildSelector_IsNotEqual()
    {
        var duration = new ObservationDuration();
        var left = new RangeSelector(1).MeasureSelectorDuration(duration, TimeProvider.System);
        var right = new RangeSelector(2).MeasureSelectorDuration(duration, TimeProvider.System);

        left.ShouldNotBe(right);
    }

    [Fact]
    public void ObservableSelector_WithSameObserverInstance_IsEqual()
    {
        var observer = new ActionSelectorObserver<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>((_, _, _, _, _, _) => { });
        var left = new RangeSelector(1).ObserveWith(observer);
        var right = new RangeSelector(1).ObserveWith(observer);

        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void ObservableSelector_WithDifferentChildSelector_IsNotEqual()
    {
        var observer = new ActionSelectorObserver<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>((_, _, _, _, _, _) => { });
        var left = new RangeSelector(1).ObserveWith(observer);
        var right = new RangeSelector(2).ObserveWith(observer);

        left.ShouldNotBe(right);
    }

    [Fact]
    public void ObservableSelector_WithSeparatelyConstructedActionObservers_IsNotEqual()
    {
        var left = new RangeSelector(1).ObserveWith((IReadOnlyList<EvaluatedCandidate<int>> _) => { });
        var right = new RangeSelector(1).ObserveWith((IReadOnlyList<EvaluatedCandidate<int>> _) => { });

        left.ShouldNotBe(right);
    }

    [Fact]
    public void NestedSelectorComposition_WithEqualParts_IsEqual()
    {
        var left = ChooseOneSelector.Create(new RangeSelector(1).WithElites(2), new RangeSelector(2));
        var right = ChooseOneSelector.Create(new RangeSelector(1).WithElites(2), new RangeSelector(2));

        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void NestedSelectorComposition_WithDifferentNestedChildSelector_IsNotEqual()
    {
        var left = ChooseOneSelector.Create(new RangeSelector(1).WithElites(2), new RangeSelector(2));
        var right = ChooseOneSelector.Create(new RangeSelector(9).WithElites(2), new RangeSelector(2));

        left.ShouldNotBe(right);
    }

    private sealed record RangeSelector(int Offset) : StatelessSelector<int, DummySearchSpace<int>>
    {
        public override IReadOnlyList<EvaluatedCandidate<int>> Select(IReadOnlyList<EvaluatedCandidate<int>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace) =>
            population.Skip(Offset).Take(count).ToArray();
    }

    private sealed record FirstOfSelector
        : MultiSelector<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>
    {
        public FirstOfSelector(IReadOnlyList<ISelector<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>> childSelectors)
            : base(childSelectors)
        {
        }

        protected override MultiSelectorInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>> CreateExecutionInstance(ImmutableArray<ISelectorInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>> childSelectors) =>
            new Instance(childSelectors);

        private sealed class Instance(ImmutableArray<ISelectorInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>> childSelectors)
            : MultiSelectorInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(childSelectors)
        {
            public override IReadOnlyList<EvaluatedCandidate<int>> Select(IReadOnlyList<EvaluatedCandidate<int>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace, IProblem<int, DummySearchSpace<int>> problem) =>
                ChildSelectors[0].Select(population, objective, count, random, searchSpace, problem);
        }
    }
}
