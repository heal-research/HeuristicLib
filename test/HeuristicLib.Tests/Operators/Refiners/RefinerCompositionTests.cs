using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Refiners;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Operators.Refiners;

public class RefinerCompositionTests
{
    [Fact]
    public void PipelineRefiner_AppliesChildRefinersInConfiguredOrder()
    {
        var addThenDouble = PipelineRefiner.Create(AddOffset(1), Multiply(2)).CreateExecutionInstance();
        var doubleThenAdd = PipelineRefiner.Create(Multiply(2), AddOffset(1)).CreateExecutionInstance();

        Refine(addThenDouble, 3).ShouldBe([8]);
        Refine(doubleThenAdd, 3).ShouldBe([7]);
    }

    [Fact]
    public void PipelineRefiner_AppliesARepeatedStageEveryTimeItAppears()
    {
        var simplify = AddOffset(1);
        var instance = PipelineRefiner.Create(simplify, Multiply(2), simplify).CreateExecutionInstance();

        Refine(instance, 3).ShouldBe([9]);
    }

    [Fact]
    public void PipelineRefiner_WithoutChildRefiners_ReturnsCandidatesUnchanged()
    {
        var instance = PipelineRefiner.Create<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>().CreateExecutionInstance();

        Refine(instance, 3, 4).ShouldBe([3, 4]);
    }

    [Fact]
    public void IteratedRefiner_AppliesTheChildRefinerOncePerIteration()
    {
        var counter = new ObservationCounter();
        var instance = AddOffset(1).CountRefinerCalls(counter).AsIterated(4).CreateExecutionInstance();

        Refine(instance, 3).ShouldBe([7]);
        counter.CurrentCount.ShouldBe(4);
    }

    [Fact]
    public void IteratedRefiner_WithNonPositiveIterations_ThrowsWhenCreatingTheExecutionInstance()
    {
        var refiner = AddOffset(1).AsIterated(0);

        Should.Throw<InvalidOperationException>(() => refiner.CreateExecutionInstance());
    }

    [Fact]
    public void IteratedRefiner_RetainsItsChildRefinerAndIterations()
    {
        var child = AddOffset(1);
        var refiner = child.AsIterated(3);

        refiner.ChildRefiner.ShouldBeSameAs(child);
        refiner.Iterations.ShouldBe(3);
    }

    [Fact]
    public void ChooseOneRefiner_WithoutChildRefiners_ThrowsWhenCreatingTheExecutionInstance()
    {
        var refiner = ChooseOneRefiner.Create<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>();

        Should.Throw<InvalidOperationException>(() => refiner.CreateExecutionInstance());
    }

    [Fact]
    public void ChooseOneRefiner_WithMismatchedWeightCount_ThrowsWhenCreatingTheExecutionInstance()
    {
        var refiner = ChooseOneRefiner.Create([AddOffset(1), Multiply(2)], [1.0]);

        Should.Throw<InvalidOperationException>(() => refiner.CreateExecutionInstance());
    }

    [Fact]
    public void ChooseOneRefiner_WithZeroWeightForAChild_NeverSelectsThatChild()
    {
        var instance = ChooseOneRefiner.Create([AddOffset(1), Multiply(2)], [1.0, 0.0]).CreateExecutionInstance();

        Refine(instance, 3, 3, 3, 3).ShouldBe([4, 4, 4, 4]);
    }

    [Fact]
    public void WithRate_OfZero_LeavesEveryCandidateUnchanged()
    {
        var instance = AddOffset(1).WithRate(0.0).CreateExecutionInstance();

        Refine(instance, 3, 4, 5).ShouldBe([3, 4, 5]);
    }

    [Fact]
    public void NoChangeRefiner_ReturnsTheSuppliedCandidates()
    {
        NoChangeRefiner<int>.Instance.Refine([3, 4, 5], RandomNumberGenerator.Create(1)).ShouldBe([3, 4, 5]);
    }

    [Fact]
    public void CountRefinedCandidates_CountsEveryReturnedCandidate()
    {
        var counter = new ObservationCounter();
        var instance = AddOffset(1).CountRefinedCandidates(counter).CreateExecutionInstance();

        Refine(instance, 3, 4, 5);

        counter.CurrentCount.ShouldBe(3);
    }

    [Fact]
    public void CountRefinerCalls_DoesNotIncrementWhenRefinementThrows()
    {
        var counter = new ObservationCounter();
        var instance = new ThrowingRefiner().CountRefinerCalls(counter).CreateExecutionInstance();

        Should.Throw<InvalidOperationException>(() => Refine(instance, 3));

        counter.CurrentCount.ShouldBe(0);
    }

    [Fact]
    public void MeasureRefinerDuration_RecordsElapsedDurationWhenRefinementThrows()
    {
        var duration = new ObservationDuration();
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var instance = new ThrowingRefiner().MeasureRefinerDuration(duration, timeProvider).CreateExecutionInstance();

        Should.Throw<InvalidOperationException>(() => Refine(instance, 3));

        duration.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void ObservableRefiner_ReportsRefinedAndOriginalCandidates()
    {
        IReadOnlyList<int>? observedRefined = null;
        IReadOnlyList<int>? observedCandidates = null;
        var instance = AddOffset(1)
            .ObserveWith((refined, candidates, _, _) =>
            {
                observedRefined = refined;
                observedCandidates = candidates;
            })
            .CreateExecutionInstance();

        Refine(instance, 3, 4);

        observedRefined.ShouldBe([4, 5]);
        observedCandidates.ShouldBe([3, 4]);
    }

    [Fact]
    public void ObservableRefiner_DoesNotInvokeObserversWhenRefinementThrows()
    {
        var observed = 0;
        var instance = new ThrowingRefiner().ObserveWith((IReadOnlyList<int> _) => observed++).CreateExecutionInstance();

        Should.Throw<InvalidOperationException>(() => Refine(instance, 3));

        observed.ShouldBe(0);
    }

    [Fact]
    public void IteratedRefiner_WithOneIteration_MatchesTheBareRefiner()
    {
        var iterated = AddOffset(1).AsIterated(1).CreateExecutionInstance();

        Refine(iterated, 3, 4).ShouldBe(Refine(AddOffset(1).CreateExecutionInstance(), 3, 4));
    }

    // Instrumentation reports what its own position sees, so the same counter says something different inside an
    // iterated refiner than around it.
    [Fact]
    public void ObservableRefiner_ReportsOncePerCallAtItsOwnPositionInTheComposition()
    {
        var inside = new ObservationCounter();
        var around = new ObservationCounter();
        var insideInstance = AddOffset(1).CountRefinerCalls(inside).AsIterated(3).CreateExecutionInstance();
        var aroundInstance = AddOffset(1).AsIterated(3).CountRefinerCalls(around).CreateExecutionInstance();

        Refine(insideInstance, 3).ShouldBe([6]);
        Refine(aroundInstance, 3).ShouldBe([6]);

        inside.CurrentCount.ShouldBe(3);
        around.CurrentCount.ShouldBe(1);
    }

    [Fact]
    public void NestedComposition_AppliesEveryStageAndItsInstrumentation()
    {
        var counter = new ObservationCounter();
        var instance = PipelineRefiner.Create(
                AddOffset(1).CountRefinedCandidates(counter),
                Multiply(2).AsIterated(2))
            .CreateExecutionInstance();

        Refine(instance, 3).ShouldBe([16]);
        counter.CurrentCount.ShouldBe(1);
    }

    [Fact]
    public void ChooseOneRefiner_WithTheSameSeed_MakesTheSameSelections()
    {
        var refiner = ChooseOneRefiner.Create([AddOffset(1), Multiply(2)], [1.0, 1.0]);
        var candidates = new[] { 3, 3, 3, 3, 3, 3, 3, 3 };

        var first = refiner.CreateExecutionInstance().Refine(candidates, RandomNumberGenerator.Create(7), DummySearchSpace<int>.Instance, CreateProblem());
        var second = refiner.CreateExecutionInstance().Refine(candidates, RandomNumberGenerator.Create(7), DummySearchSpace<int>.Instance, CreateProblem());

        second.ShouldBe(first);
        // Both children are actually reachable, or the comparison above would be vacuous.
        first.Distinct().Count().ShouldBe(2);
    }

    [Fact]
    public void WithRate_OfOne_RefinesEveryCandidate()
    {
        var instance = AddOffset(1).WithRate(1.0).CreateExecutionInstance();

        Refine(instance, 3, 4, 5).ShouldBe([4, 5, 6]);
    }

    private static AddOffsetRefiner AddOffset(int offset) => new(offset);

    private static MultiplyRefiner Multiply(int factor) => new(factor);

    private static IReadOnlyList<int> Refine(IRefinerInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>> instance, params int[] candidates) =>
        instance.Refine(candidates, RandomNumberGenerator.Create(42), DummySearchSpace<int>.Instance, CreateProblem());

    private static FuncProblem<int, DummySearchSpace<int>> CreateProblem() =>
        FuncProblem.Create(static (int candidate) => candidate, DummySearchSpace<int>.Instance, SingleObjective.Minimize);

    private sealed record AddOffsetRefiner(int Offset) : SingleCandidateRefiner<int, DummySearchSpace<int>>
    {
        public override int RefineCandidate(int candidate, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace) => candidate + Offset;
    }

    private sealed record MultiplyRefiner(int Factor) : SingleCandidateRefiner<int, DummySearchSpace<int>>
    {
        public override int RefineCandidate(int candidate, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace) => candidate * Factor;
    }

    private sealed record ThrowingRefiner : StatelessRefiner<int, DummySearchSpace<int>>
    {
        public override IReadOnlyList<int> Refine(IReadOnlyList<int> candidates, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace) =>
            throw new InvalidOperationException("Refinement failed.");
    }

    private sealed class AdvancingTimeProvider(TimeSpan step) : TimeProvider
    {
        private long timestamp;

        public override long GetTimestamp()
        {
            var current = timestamp;
            timestamp += (long)(step.TotalSeconds * TimestampFrequency);
            return current;
        }
    }
}
