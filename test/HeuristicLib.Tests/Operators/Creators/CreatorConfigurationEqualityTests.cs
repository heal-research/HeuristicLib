using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Operators.Creators;

/// <summary>
/// Pins the child slot of the creator topology bases: the configured children are publicly inspectable and the
/// child collection compares by ordered structural equality rather than by array reference.
/// </summary>
public class CreatorConfigurationEqualityTests
{
    [Fact]
    public void WrappingCreator_ExposesConfiguredChildCreator()
    {
        var childCreator = new ConstantCreator(1);

        var creator = childCreator.CountCreatorCalls(new ObservationCounter());

        creator.ChildCreator.ShouldBeSameAs(childCreator);
    }

    [Fact]
    public void MultiCreator_ExposesConfiguredChildCreators()
    {
        var first = new ConstantCreator(1);
        var second = new ConstantCreator(2);

        var creator = ChooseOneCreator.Create(first, second);

        creator.ChildCreators.ShouldBe([first, second]);
    }

    [Fact]
    public void MultiCreator_SnapshotsConfiguredChildCreators()
    {
        var first = new ConstantCreator(1);
        var second = new ConstantCreator(2);
        var childCreators = new List<ICreator<int>> { first, second };
        var creator = new FirstOfCreator(childCreators);

        childCreators.Clear();

        creator.ChildCreators.ShouldBe([first, second]);
    }

    [Fact]
    public void MultiCreator_WithSeparatelyBuiltEqualChildCollections_IsEqual()
    {
        var left = ChooseOneCreator.Create(new ConstantCreator(1), new ConstantCreator(2));
        var right = ChooseOneCreator.Create(new ConstantCreator(1), new ConstantCreator(2));

        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void MultiCreator_WithDifferentChildCreators_IsNotEqual()
    {
        var left = ChooseOneCreator.Create(new ConstantCreator(1), new ConstantCreator(2));
        var right = ChooseOneCreator.Create(new ConstantCreator(1), new ConstantCreator(3));

        left.ShouldNotBe(right);
    }

    [Fact]
    public void MultiCreator_WithReorderedChildCreators_IsNotEqual()
    {
        var left = ChooseOneCreator.Create(new ConstantCreator(1), new ConstantCreator(2));
        var right = ChooseOneCreator.Create(new ConstantCreator(2), new ConstantCreator(1));

        left.ShouldNotBe(right);
    }

    [Fact]
    public void MultiCreator_WithDifferentChildCount_IsNotEqual()
    {
        var left = ChooseOneCreator.Create(new ConstantCreator(1), new ConstantCreator(2));
        var right = ChooseOneCreator.Create(new ConstantCreator(1));

        left.ShouldNotBe(right);
    }

    [Fact]
    public void ChooseOneCreator_WithDifferentWeights_IsNotEqual()
    {
        ICreator<int>[] childCreators = [new ConstantCreator(1), new ConstantCreator(2)];
        var left = new ChooseOneCreator<int>(childCreators) { Weights = [1.0, 2.0] };
        var right = new ChooseOneCreator<int>(childCreators) { Weights = [2.0, 1.0] };

        left.ShouldNotBe(right);
    }

    /// <summary>
    /// Omitted weights are retained as an empty collection meaning uniform selection, so an explicitly uniform
    /// configuration stays distinguishable from an unweighted one.
    /// </summary>
    [Fact]
    public void ChooseOneCreator_WithOmittedWeights_IsNotEqualToExplicitUniformWeights()
    {
        ICreator<int>[] childCreators = [new ConstantCreator(1), new ConstantCreator(2)];
        var omitted = new ChooseOneCreator<int>(childCreators);
        var explicitUniform = new ChooseOneCreator<int>(childCreators) { Weights = [0.5, 0.5] };

        omitted.Weights.ShouldBeEmpty();
        omitted.ShouldNotBe(explicitUniform);
    }

    [Fact]
    public void MultiCreatorTopologies_WithEqualChildCreators_AreNotEqualAcrossTypes()
    {
        var chooseOne = ChooseOneCreator.Create(new ConstantCreator(1), new ConstantCreator(2));
        var firstOf = new FirstOfCreator([new ConstantCreator(1), new ConstantCreator(2)]);

        chooseOne.ShouldNotBe<object>(firstOf);
    }

    /// <summary>
    /// Operators whose children carry a specific role declare those children themselves instead of deriving from a
    /// topology base, so their equality comes from the named properties.
    /// </summary>
    [Fact]
    public void PredefinedCandidatesCreator_ExposesItsFallbackCreatorUnderOneName()
    {
        var fallback = new ConstantCreator(1);

        var creator = fallback.WithPredefinedCandidates([7, 8]);

        creator.CreatorForRemainingCandidates.ShouldBeSameAs(fallback);
        creator.PredefinedCandidates.ShouldBe([7, 8]);
    }

    [Fact]
    public void PredefinedCandidatesCreator_WithEqualParts_IsEqual()
    {
        var left = new ConstantCreator(1).WithPredefinedCandidates([7, 8]);
        var right = new ConstantCreator(1).WithPredefinedCandidates([7, 8]);

        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void PredefinedCandidatesCreator_WithDifferentFallbackCreator_IsNotEqual()
    {
        var left = new ConstantCreator(1).WithPredefinedCandidates([7, 8]);
        var right = new ConstantCreator(2).WithPredefinedCandidates([7, 8]);

        left.ShouldNotBe(right);
    }

    [Fact]
    public void PredefinedCandidatesCreator_SnapshotsPredefinedCandidates()
    {
        var predefined = new List<int> { 7, 8 };
        var creator = new ConstantCreator(1).WithPredefinedCandidates(predefined);

        predefined.Clear();

        creator.PredefinedCandidates.ShouldBe([7, 8]);
    }

    [Fact]
    public void TransformedCreator_ExposesItsSourceAndTransformation()
    {
        var source = new ConstantCreator(1);
        var transformation = new AddOneMutator();

        var creator = source.TransformWith(transformation);

        creator.SourceCreator.ShouldBeSameAs(source);
        creator.TransformationMutator.ShouldBeSameAs(transformation);
    }

    [Fact]
    public void CountingCreator_WithSameCounterAndMetric_IsEqual()
    {
        var counter = new ObservationCounter();
        var left = new ConstantCreator(1).CountCreatorCalls(counter);
        var right = new ConstantCreator(1).CountCreatorCalls(counter);

        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void CountingCreator_WithDifferentMetric_IsNotEqual()
    {
        var counter = new ObservationCounter();
        var left = new ConstantCreator(1).CountCreatorCalls(counter);
        var right = new ConstantCreator(1).CountCreatedCandidates(counter);

        left.ShouldNotBe(right);
    }

    [Fact]
    public void CountingCreator_WithDifferentCounter_IsNotEqual()
    {
        var left = new ConstantCreator(1).CountCreatorCalls(new ObservationCounter());
        var right = new ConstantCreator(1).CountCreatorCalls(new ObservationCounter());

        left.ShouldNotBe(right);
    }

    [Fact]
    public void CountingCreator_WithDifferentChildCreator_IsNotEqual()
    {
        var counter = new ObservationCounter();
        var left = new ConstantCreator(1).CountCreatorCalls(counter);
        var right = new ConstantCreator(2).CountCreatorCalls(counter);

        left.ShouldNotBe(right);
    }

    [Fact]
    public void DurationMeasuringCreator_WithSameDurationAndTimeProvider_IsEqual()
    {
        var duration = new ObservationDuration();
        var left = new ConstantCreator(1).MeasureCreatorDuration(duration, TimeProvider.System);
        var right = new ConstantCreator(1).MeasureCreatorDuration(duration, TimeProvider.System);

        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void DurationMeasuringCreator_WithDifferentDuration_IsNotEqual()
    {
        var left = new ConstantCreator(1).MeasureCreatorDuration(new ObservationDuration(), TimeProvider.System);
        var right = new ConstantCreator(1).MeasureCreatorDuration(new ObservationDuration(), TimeProvider.System);

        left.ShouldNotBe(right);
    }

    [Fact]
    public void DurationMeasuringCreator_WithDifferentChildCreator_IsNotEqual()
    {
        var duration = new ObservationDuration();
        var left = new ConstantCreator(1).MeasureCreatorDuration(duration, TimeProvider.System);
        var right = new ConstantCreator(2).MeasureCreatorDuration(duration, TimeProvider.System);

        left.ShouldNotBe(right);
    }

    [Fact]
    public void ObservableCreator_WithSameObserverInstance_IsEqual()
    {
        var observer = new ActionCreatorObserver<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>((_, _, _, _) => { });
        var left = new ConstantCreator(1).ObserveWith(observer);
        var right = new ConstantCreator(1).ObserveWith(observer);

        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void ObservableCreator_WithDifferentChildCreator_IsNotEqual()
    {
        var observer = new ActionCreatorObserver<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>((_, _, _, _) => { });
        var left = new ConstantCreator(1).ObserveWith(observer);
        var right = new ConstantCreator(2).ObserveWith(observer);

        left.ShouldNotBe(right);
    }

    [Fact]
    public void ObservableCreator_WithSeparatelyConstructedActionObservers_IsNotEqual()
    {
        var left = new ConstantCreator(1).ObserveWith((IReadOnlyList<int> _) => { });
        var right = new ConstantCreator(1).ObserveWith((IReadOnlyList<int> _) => { });

        left.ShouldNotBe(right);
    }

    [Fact]
    public void SingleCandidateCreator_WithDifferentConcurrency_IsNotEqual()
    {
        var left = new ConstantCreator(1);
        var right = left with { Concurrency = ExecutionConcurrency.Concurrent(2) };

        left.ShouldNotBe(right);
    }

    [Fact]
    public void NestedCreatorComposition_WithEqualParts_IsEqual()
    {
        var left = ChooseOneCreator.Create(new ConstantCreator(1).TransformWith(new AddOneMutator()), new ConstantCreator(2));
        var right = ChooseOneCreator.Create(new ConstantCreator(1).TransformWith(new AddOneMutator()), new ConstantCreator(2));

        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void NestedCreatorComposition_WithDifferentNestedChildCreator_IsNotEqual()
    {
        var left = ChooseOneCreator.Create(new ConstantCreator(1).TransformWith(new AddOneMutator()), new ConstantCreator(2));
        var right = ChooseOneCreator.Create(new ConstantCreator(9).TransformWith(new AddOneMutator()), new ConstantCreator(2));

        left.ShouldNotBe(right);
    }

    private sealed record ConstantCreator(int Value) : SingleCandidateCreator<int, DummySearchSpace<int>>
    {
        public override int CreateCandidate(IRandomNumberGenerator random, DummySearchSpace<int> searchSpace) => Value;
    }

    private sealed record AddOneMutator : SingleCandidateMutator<int, DummySearchSpace<int>>
    {
        public override int MutateCandidate(int parent, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace) => parent + 1;
    }

    private sealed record FirstOfCreator
        : MultiCreator<int>
    {
        public FirstOfCreator(IReadOnlyList<ICreator<int>> childCreators)
            : base(childCreators)
        {
        }

        protected override ICreatorInstance<int, TRunSearchSpace, TRunProblem> CombineExecutionInstances<TRunSearchSpace, TRunProblem>(ImmutableArray<ICreatorInstance<int, TRunSearchSpace, TRunProblem>> childCreators) =>
            new Instance<TRunSearchSpace, TRunProblem>(childCreators);

        private sealed class Instance<TSearchSpace, TProblem>(ImmutableArray<ICreatorInstance<int, TSearchSpace, TProblem>> childCreators)
            : MultiCreatorInstance<int, TSearchSpace, TProblem>(childCreators)
            where TSearchSpace : class, ISearchSpace<int>
            where TProblem : class, IProblem<int, TSearchSpace>
        {
            public override IReadOnlyList<int> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
                ChildCreators[0].Create(count, random, searchSpace, problem);
        }
    }
}
