using HEAL.HeuristicLib.Operators.Refiners;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Operators.Refiners;

public class RefinerConfigurationEqualityTests
{
    [Fact]
    public void PipelineRefiner_WithEqualChildRefiners_IsEqual()
    {
        var left = PipelineRefiner.Create(new AddOffsetRefiner(1), new AddOffsetRefiner(2));
        var right = PipelineRefiner.Create(new AddOffsetRefiner(1), new AddOffsetRefiner(2));

        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void PipelineRefiner_WithDifferentChildRefiners_IsNotEqual()
    {
        var left = PipelineRefiner.Create(new AddOffsetRefiner(1), new AddOffsetRefiner(2));
        var right = PipelineRefiner.Create(new AddOffsetRefiner(1), new AddOffsetRefiner(3));

        left.ShouldNotBe(right);
    }

    [Fact]
    public void PipelineRefiner_WithReorderedChildRefiners_IsNotEqual()
    {
        var left = PipelineRefiner.Create(new AddOffsetRefiner(1), new AddOffsetRefiner(2));
        var right = PipelineRefiner.Create(new AddOffsetRefiner(2), new AddOffsetRefiner(1));

        left.ShouldNotBe(right);
    }

    [Fact]
    public void PipelineRefiner_WithDifferentChildCount_IsNotEqual()
    {
        var left = PipelineRefiner.Create(new AddOffsetRefiner(1), new AddOffsetRefiner(2));
        var right = PipelineRefiner.Create(new AddOffsetRefiner(1));

        left.ShouldNotBe(right);
    }

    [Fact]
    public void ChooseOneRefiner_WithDifferentChildRefiners_IsNotEqual()
    {
        var left = ChooseOneRefiner.Create([new AddOffsetRefiner(1)], [1.0]);
        var right = ChooseOneRefiner.Create([new AddOffsetRefiner(2)], [1.0]);

        left.ShouldNotBe(right);
    }

    [Fact]
    public void ChooseOneRefiner_WithDifferentWeights_IsNotEqual()
    {
        var children = new IRefiner<int>[]
        {
            new AddOffsetRefiner(1),
            new AddOffsetRefiner(2),
        };
        var left = ChooseOneRefiner.Create(children, [1.0, 2.0]);
        var right = ChooseOneRefiner.Create(children, [2.0, 1.0]);

        left.ShouldNotBe(right);
    }

    [Fact]
    public void MultiRefinerTopologies_WithEqualChildRefiners_AreNotEqualAcrossTypes()
    {
        var pipeline = PipelineRefiner.Create(new AddOffsetRefiner(1), new AddOffsetRefiner(2));
        var chooseOne = ChooseOneRefiner.Create(new AddOffsetRefiner(1), new AddOffsetRefiner(2));

        pipeline.ShouldNotBe<object>(chooseOne);
    }

    [Fact]
    public void CountingRefiner_WithSameCounterAndMetric_IsEqual()
    {
        var counter = new ObservationCounter();
        var left = new AddOffsetRefiner(1).CountCalls(counter);
        var right = new AddOffsetRefiner(1).CountCalls(counter);

        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void CountingRefiner_WithDifferentMetric_IsNotEqual()
    {
        var counter = new ObservationCounter();
        var left = new AddOffsetRefiner(1).CountCalls(counter);
        var right = new AddOffsetRefiner(1).CountCandidates(counter);

        left.ShouldNotBe(right);
    }

    [Fact]
    public void CountingRefiner_WithDifferentCounter_IsNotEqual()
    {
        var left = new AddOffsetRefiner(1).CountCalls(new ObservationCounter());
        var right = new AddOffsetRefiner(1).CountCalls(new ObservationCounter());

        left.ShouldNotBe(right);
    }

    [Fact]
    public void CountingRefiner_WithDifferentChildRefiner_IsNotEqual()
    {
        var counter = new ObservationCounter();
        var left = new AddOffsetRefiner(1).CountCalls(counter);
        var right = new AddOffsetRefiner(2).CountCalls(counter);

        left.ShouldNotBe(right);
    }

    [Fact]
    public void DurationMeasuringRefiner_WithSameDurationAndTimeProvider_IsEqual()
    {
        var duration = new ObservationDuration();
        var left = new AddOffsetRefiner(1).MeasureDuration(duration, TimeProvider.System);
        var right = new AddOffsetRefiner(1).MeasureDuration(duration, TimeProvider.System);

        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void DurationMeasuringRefiner_WithDifferentDuration_IsNotEqual()
    {
        var left = new AddOffsetRefiner(1).MeasureDuration(new ObservationDuration());
        var right = new AddOffsetRefiner(1).MeasureDuration(new ObservationDuration());

        left.ShouldNotBe(right);
    }

    [Fact]
    public void DurationMeasuringRefiner_WithDifferentChildRefiner_IsNotEqual()
    {
        var duration = new ObservationDuration();
        var left = new AddOffsetRefiner(1).MeasureDuration(duration);
        var right = new AddOffsetRefiner(2).MeasureDuration(duration);

        left.ShouldNotBe(right);
    }

    [Fact]
    public void ObservableRefiner_WithSameObserverInstance_IsEqual()
    {
        var observer = new ActionRefinerObserver<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>((_, _, _, _) => { });
        var left = new AddOffsetRefiner(1).ObserveWith(observer);
        var right = new AddOffsetRefiner(1).ObserveWith(observer);

        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void ObservableRefiner_WithDifferentChildRefiner_IsNotEqual()
    {
        var observer = new ActionRefinerObserver<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>((_, _, _, _) => { });
        var left = new AddOffsetRefiner(1).ObserveWith(observer);
        var right = new AddOffsetRefiner(2).ObserveWith(observer);

        left.ShouldNotBe(right);
    }

    // A freshly allocated action observer defeats structural equality, which is why concern types carrying only
    // value-like settings must not be expressed as an observer wrapper.
    [Fact]
    public void ObservableRefiner_WithSeparatelyConstructedActionObservers_IsNotEqual()
    {
        var left = new AddOffsetRefiner(1).ObserveWith(_ => { });
        var right = new AddOffsetRefiner(1).ObserveWith(_ => { });

        left.ShouldNotBe(right);
    }

    [Fact]
    public void IteratedRefiner_WithEqualChildRefinerAndIterations_IsEqual()
    {
        var left = new AddOffsetRefiner(1).AsIterated(3);
        var right = new AddOffsetRefiner(1).AsIterated(3);

        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void IteratedRefiner_WithDifferentIterations_IsNotEqual()
    {
        var left = new AddOffsetRefiner(1).AsIterated(3);
        var right = new AddOffsetRefiner(1).AsIterated(4);

        left.ShouldNotBe(right);
    }

    [Fact]
    public void IteratedRefiner_WithDifferentChildRefiner_IsNotEqual()
    {
        var left = new AddOffsetRefiner(1).AsIterated(3);
        var right = new AddOffsetRefiner(2).AsIterated(3);

        left.ShouldNotBe(right);
    }

    [Fact]
    public void SingleCandidateRefiner_WithDifferentConcurrency_IsNotEqual()
    {
        var left = new AddOffsetRefiner(1);
        var right = left with { Concurrency = ExecutionConcurrency.Concurrent() };

        left.ShouldNotBe(right);
    }

    [Fact]
    public void NestedRefinerComposition_WithEqualParts_IsEqual()
    {
        var counter = new ObservationCounter();
        var left = PipelineRefiner.Create(
            new AddOffsetRefiner(1).CountCalls(counter),
            new AddOffsetRefiner(2));
        var right = PipelineRefiner.Create(
            new AddOffsetRefiner(1).CountCalls(counter),
            new AddOffsetRefiner(2));

        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void NestedRefinerComposition_WithDifferentNestedChildRefiner_IsNotEqual()
    {
        var counter = new ObservationCounter();
        var left = PipelineRefiner.Create(
            new AddOffsetRefiner(1).CountCalls(counter),
            new AddOffsetRefiner(2));
        var right = PipelineRefiner.Create(
            new AddOffsetRefiner(9).CountCalls(counter),
            new AddOffsetRefiner(2));

        left.ShouldNotBe(right);
    }

    [Fact]
    public void HandwrittenMultiRefiner_ComparesChildRefinersWithoutAnyEqualityDeclaration()
    {
        var left = new FirstOfRefiner([new AddOffsetRefiner(1), new AddOffsetRefiner(2)]);
        var right = new FirstOfRefiner([new AddOffsetRefiner(1), new AddOffsetRefiner(2)]);

        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
        left.ShouldNotBe(new FirstOfRefiner([new AddOffsetRefiner(2), new AddOffsetRefiner(1)]));
        left.ShouldNotBe(new FirstOfRefiner([new AddOffsetRefiner(1)]));
    }

    [Fact]
    public void HandwrittenMultiRefiner_SnapshotsConfiguredChildRefiners()
    {
        var first = new AddOffsetRefiner(1);
        var second = new AddOffsetRefiner(2);
        var childRefiners = new List<IRefiner<int>> { first, second };
        var refiner = new FirstOfRefiner(childRefiners);

        childRefiners.Clear();

        refiner.ChildRefiners.ShouldBe([first, second]);
    }

    private sealed record AddOffsetRefiner(int Offset) : SingleCandidateRefiner<int, DummySearchSpace<int>>
    {
        public override int RefineCandidate(int candidate, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace) => candidate + Offset;
    }

    // An externally authored topology with no equality attribute, generator or hand-written comparison: its structural
    // equality follows from ChildRefiners being a value array.
    private sealed record FirstOfRefiner
        : MultiRefiner<int>
    {
        public FirstOfRefiner(IReadOnlyList<IRefiner<int>> childRefiners)
            : base(childRefiners)
        {
        }

        protected override IRefinerInstance<int, TRunSearchSpace, TRunProblem> CombineExecutionInstances<TRunSearchSpace, TRunProblem>(ImmutableArray<IRefinerInstance<int, TRunSearchSpace, TRunProblem>> childRefiners) =>
            new Instance<TRunSearchSpace, TRunProblem>(childRefiners);

        private sealed class Instance<TSearchSpace, TProblem>(ImmutableArray<IRefinerInstance<int, TSearchSpace, TProblem>> childRefiners)
            : MultiRefinerInstance<int, TSearchSpace, TProblem>(childRefiners)
            where TSearchSpace : class, ISearchSpace<int>
            where TProblem : class, IProblem<int, TSearchSpace>
        {
            public override IReadOnlyList<int> Refine(IReadOnlyList<int> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
                ChildRefiners[0].Refine(candidates, random, searchSpace, problem);
        }
    }
}
