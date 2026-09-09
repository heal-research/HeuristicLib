using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Operators.Mutators;

/// <summary>
/// Pins structural equality of mutator configurations. The mutator slice is the reference shape for the remaining
/// roles, so every topology base and concern type is covered in both the equal and the not-equal direction.
/// </summary>
public class MutatorConfigurationEqualityTests
{
    [Fact]
    public void PipelineMutator_WithEqualChildMutators_IsEqual()
    {
        var left = PipelineMutator.Create(new AddOffsetMutator(1), new AddOffsetMutator(2));
        var right = PipelineMutator.Create(new AddOffsetMutator(1), new AddOffsetMutator(2));

        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void PipelineMutator_WithDifferentChildMutators_IsNotEqual()
    {
        var left = PipelineMutator.Create(new AddOffsetMutator(1), new AddOffsetMutator(2));
        var right = PipelineMutator.Create(new AddOffsetMutator(1), new AddOffsetMutator(3));

        left.ShouldNotBe(right);
    }

    [Fact]
    public void PipelineMutator_WithReorderedChildMutators_IsNotEqual()
    {
        var left = PipelineMutator.Create(new AddOffsetMutator(1), new AddOffsetMutator(2));
        var right = PipelineMutator.Create(new AddOffsetMutator(2), new AddOffsetMutator(1));

        left.ShouldNotBe(right);
    }

    [Fact]
    public void PipelineMutator_WithDifferentChildCount_IsNotEqual()
    {
        var left = PipelineMutator.Create(new AddOffsetMutator(1), new AddOffsetMutator(2));
        var right = PipelineMutator.Create(new AddOffsetMutator(1));

        left.ShouldNotBe(right);
    }

    [Fact]
    public void ChooseOneMutator_WithDifferentChildMutators_IsNotEqual()
    {
        var left = ChooseOneMutator.Create([new AddOffsetMutator(1)], [1.0]);
        var right = ChooseOneMutator.Create([new AddOffsetMutator(2)], [1.0]);

        left.ShouldNotBe(right);
    }

    [Fact]
    public void ChooseOneMutator_WithDifferentWeights_IsNotEqual()
    {
        var children = new IMutator<int>[]
        {
            new AddOffsetMutator(1),
            new AddOffsetMutator(2),
        };
        var left = ChooseOneMutator.Create(children, [1.0, 2.0]);
        var right = ChooseOneMutator.Create(children, [2.0, 1.0]);

        left.ShouldNotBe(right);
    }

    [Fact]
    public void MultiMutatorTopologies_WithEqualChildMutators_AreNotEqualAcrossTypes()
    {
        var pipeline = PipelineMutator.Create(new AddOffsetMutator(1), new AddOffsetMutator(2));
        var chooseOne = ChooseOneMutator.Create(new AddOffsetMutator(1), new AddOffsetMutator(2));

        pipeline.ShouldNotBe<object>(chooseOne);
    }

    [Fact]
    public void CountingMutator_WithSameCounterAndMetric_IsEqual()
    {
        var counter = new ObservationCounter();
        var left = new AddOffsetMutator(1).CountCalls(counter);
        var right = new AddOffsetMutator(1).CountCalls(counter);

        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void CountingMutator_WithDifferentMetric_IsNotEqual()
    {
        var counter = new ObservationCounter();
        var left = new AddOffsetMutator(1).CountCalls(counter);
        var right = new AddOffsetMutator(1).CountCandidates(counter);

        left.ShouldNotBe(right);
    }

    [Fact]
    public void CountingMutator_WithDifferentCounter_IsNotEqual()
    {
        var left = new AddOffsetMutator(1).CountCalls(new ObservationCounter());
        var right = new AddOffsetMutator(1).CountCalls(new ObservationCounter());

        left.ShouldNotBe(right);
    }

    [Fact]
    public void CountingMutator_WithDifferentChildMutator_IsNotEqual()
    {
        var counter = new ObservationCounter();
        var left = new AddOffsetMutator(1).CountCalls(counter);
        var right = new AddOffsetMutator(2).CountCalls(counter);

        left.ShouldNotBe(right);
    }

    [Fact]
    public void DurationMeasuringMutator_WithSameDurationAndTimeProvider_IsEqual()
    {
        var duration = new ObservationDuration();
        var left = new AddOffsetMutator(1).MeasureDuration(duration, TimeProvider.System);
        var right = new AddOffsetMutator(1).MeasureDuration(duration, TimeProvider.System);

        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void DurationMeasuringMutator_WithDifferentDuration_IsNotEqual()
    {
        var left = new AddOffsetMutator(1).MeasureDuration(new ObservationDuration());
        var right = new AddOffsetMutator(1).MeasureDuration(new ObservationDuration());

        left.ShouldNotBe(right);
    }

    [Fact]
    public void DurationMeasuringMutator_WithDifferentChildMutator_IsNotEqual()
    {
        var duration = new ObservationDuration();
        var left = new AddOffsetMutator(1).MeasureDuration(duration);
        var right = new AddOffsetMutator(2).MeasureDuration(duration);

        left.ShouldNotBe(right);
    }

    [Fact]
    public void ObservableMutator_WithSameObserverInstance_IsEqual()
    {
        var observer = new ActionMutatorObserver<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>((_, _, _, _) => { });
        var left = new AddOffsetMutator(1).ObserveWith(observer);
        var right = new AddOffsetMutator(1).ObserveWith(observer);

        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void ObservableMutator_WithDifferentChildMutator_IsNotEqual()
    {
        var observer = new ActionMutatorObserver<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>((_, _, _, _) => { });
        var left = new AddOffsetMutator(1).ObserveWith(observer);
        var right = new AddOffsetMutator(2).ObserveWith(observer);

        left.ShouldNotBe(right);
    }

    /// <summary>
    /// Documents that a freshly allocated action observer defeats structural equality, which is why concern types
    /// carrying only value-like settings must not be expressed as an observer wrapper.
    /// </summary>
    [Fact]
    public void ObservableMutator_WithSeparatelyConstructedActionObservers_IsNotEqual()
    {
        var left = new AddOffsetMutator(1).ObserveWith(_ => { });
        var right = new AddOffsetMutator(1).ObserveWith(_ => { });

        left.ShouldNotBe(right);
    }

    [Fact]
    public void SingleCandidateMutator_WithDifferentConcurrency_IsNotEqual()
    {
        var left = new AddOffsetMutator(1);
        var right = left with { Concurrency = ExecutionConcurrency.Concurrent() };

        left.ShouldNotBe(right);
    }

    [Fact]
    public void NestedMutatorComposition_WithEqualParts_IsEqual()
    {
        var counter = new ObservationCounter();
        var left = PipelineMutator.Create(
            new AddOffsetMutator(1).CountCalls(counter),
            new AddOffsetMutator(2));
        var right = PipelineMutator.Create(
            new AddOffsetMutator(1).CountCalls(counter),
            new AddOffsetMutator(2));

        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void NestedMutatorComposition_WithDifferentNestedChildMutator_IsNotEqual()
    {
        var counter = new ObservationCounter();
        var left = PipelineMutator.Create(
            new AddOffsetMutator(1).CountCalls(counter),
            new AddOffsetMutator(2));
        var right = PipelineMutator.Create(
            new AddOffsetMutator(9).CountCalls(counter),
            new AddOffsetMutator(2));

        left.ShouldNotBe(right);
    }

    [Fact]
    public void HandwrittenMultiMutator_ComparesChildMutatorsWithoutAnyEqualityDeclaration()
    {
        var left = new FirstOfMutator([new AddOffsetMutator(1), new AddOffsetMutator(2)]);
        var right = new FirstOfMutator([new AddOffsetMutator(1), new AddOffsetMutator(2)]);

        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
        left.ShouldNotBe(new FirstOfMutator([new AddOffsetMutator(2), new AddOffsetMutator(1)]));
        left.ShouldNotBe(new FirstOfMutator([new AddOffsetMutator(1)]));
    }

    [Fact]
    public void HandwrittenMultiMutator_SnapshotsConfiguredChildMutators()
    {
        var first = new AddOffsetMutator(1);
        var second = new AddOffsetMutator(2);
        var childMutators = new List<IMutator<int>> { first, second };
        var mutator = new FirstOfMutator(childMutators);

        childMutators.Clear();

        mutator.ChildMutators.ShouldBe([first, second]);
    }

    private sealed record AddOffsetMutator(int Offset) : SingleCandidateMutator<int, DummySearchSpace<int>>
    {
        public override int MutateCandidate(int parent, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace) => parent + Offset;
    }

    /// <summary>
    /// An externally authored topology carrying no equality attribute, generator or hand-written comparison. Its
    /// structural equality follows from <c>ChildMutators</c> being a value array.
    /// </summary>
    private sealed record FirstOfMutator
        : MultiMutator<int>
    {
        public FirstOfMutator(IReadOnlyList<IMutator<int>> childMutators)
            : base(childMutators)
        {
        }

        protected override IMutatorInstance<int, TRunSearchSpace, TRunProblem> CombineExecutionInstances<TRunSearchSpace, TRunProblem>(ImmutableArray<IMutatorInstance<int, TRunSearchSpace, TRunProblem>> childMutators) =>
            new Instance<TRunSearchSpace, TRunProblem>(childMutators);

        private sealed class Instance<TSearchSpace, TProblem>(ImmutableArray<IMutatorInstance<int, TSearchSpace, TProblem>> childMutators)
            : MultiMutatorInstance<int, TSearchSpace, TProblem>(childMutators)
            where TSearchSpace : class, ISearchSpace<int>
            where TProblem : class, IProblem<int, TSearchSpace>
        {
            public override IReadOnlyList<int> Mutate(IReadOnlyList<int> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
                ChildMutators[0].Mutate(parents, random, searchSpace, problem);
        }
    }
}
