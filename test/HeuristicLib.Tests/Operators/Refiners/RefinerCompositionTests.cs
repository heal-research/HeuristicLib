using HEAL.HeuristicLib.Operators.Refiners;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Operators.Refiners;

public class RefinerCompositionTests
{
    [Fact]
    public void PipelineRefiner_AppliesChildRefinersInConfiguredOrder()
    {
        var addThenDouble = PipelineRefiner.Create(AddOffset(1), Multiply(2)).CreateExecutionInstance<DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(ResolutionScope.Create());
        var doubleThenAdd = PipelineRefiner.Create(Multiply(2), AddOffset(1)).CreateExecutionInstance<DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(ResolutionScope.Create());

        Refine(addThenDouble, 3).ShouldBe([8]);
        Refine(doubleThenAdd, 3).ShouldBe([7]);
    }

    [Fact]
    public void PipelineRefiner_AppliesARepeatedStageEveryTimeItAppears()
    {
        var simplify = AddOffset(1);
        var execution = PipelineRefiner.Create(simplify, Multiply(2), simplify).CreateExecutionInstance<DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(ResolutionScope.Create());

        Refine(execution, 3).ShouldBe([9]);
    }

    [Fact]
    public void PipelineRefiner_WithoutChildRefiners_ReturnsCandidatesUnchanged()
    {
        var execution = PipelineRefiner.Create<int>().CreateExecutionInstance<DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(ResolutionScope.Create());

        Refine(execution, 3, 4).ShouldBe([3, 4]);
    }

    [Fact]
    public void IteratedRefiner_AppliesTheChildRefinerOncePerIteration()
    {
        var counter = new CountAccumulator();
        var execution = AddOffset(1).CountCalls(counter).AsIterated(4).CreateExecutionInstance<DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(ResolutionScope.Create());

        Refine(execution, 3).ShouldBe([7]);
        counter.CurrentCount.ShouldBe(4);
    }

    [Fact]
    public void IteratedRefiner_WithNonPositiveIterations_ThrowsWhenCreatingTheExecution()
    {
        var refiner = AddOffset(1).AsIterated(0);

        Should.Throw<InvalidOperationException>(() => refiner.CreateExecutionInstance<DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(ResolutionScope.Create()));
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
    public void ChooseOneRefiner_WithoutChildRefiners_ThrowsWhenCreatingTheExecution()
    {
        var refiner = ChooseOneRefiner.Create<int>();

        Should.Throw<InvalidOperationException>(() => refiner.CreateExecutionInstance<DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(ResolutionScope.Create()));
    }

    [Fact]
    public void ChooseOneRefiner_WithMismatchedWeightCount_ThrowsWhenCreatingTheExecution()
    {
        var refiner = ChooseOneRefiner.Create([AddOffset(1), Multiply(2)], [1.0]);

        Should.Throw<InvalidOperationException>(() => refiner.CreateExecutionInstance<DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(ResolutionScope.Create()));
    }

    [Fact]
    public void ChooseOneRefiner_WithZeroWeightForAChild_NeverSelectsThatChild()
    {
        var execution = ChooseOneRefiner.Create([AddOffset(1), Multiply(2)], [1.0, 0.0]).CreateExecutionInstance<DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(ResolutionScope.Create());

        Refine(execution, 3, 3, 3, 3).ShouldBe([4, 4, 4, 4]);
    }

    [Fact]
    public void WithRate_OfZero_LeavesEveryCandidateUnchanged()
    {
        var execution = AddOffset(1).AppliedAtRate(0.0).CreateExecutionInstance<DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(ResolutionScope.Create());

        Refine(execution, 3, 4, 5).ShouldBe([3, 4, 5]);
    }

    [Fact]
    public void NoChangeRefiner_ReturnsTheSuppliedCandidates()
    {
        NoChangeRefiner<int>.Instance.Refine([3, 4, 5], RandomNumberGenerator.Create(1)).ShouldBe([3, 4, 5]);
    }

    [Fact]
    public void CountRefinedCandidates_CountsEveryReturnedCandidate()
    {
        var counter = new CountAccumulator();
        var execution = AddOffset(1).CountCandidates(counter).CreateExecutionInstance<DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(ResolutionScope.Create());

        Refine(execution, 3, 4, 5);

        counter.CurrentCount.ShouldBe(3);
    }

    [Fact]
    public void CountRefinerCalls_DoesNotIncrementWhenRefinementThrows()
    {
        var counter = new CountAccumulator();
        var execution = new ThrowingRefiner().CountCalls(counter).CreateExecutionInstance<DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(ResolutionScope.Create());

        Should.Throw<InvalidOperationException>(() => Refine(execution, 3));

        counter.CurrentCount.ShouldBe(0);
    }

    [Fact]
    public void MeasureRefinerDuration_RecordsElapsedDurationWhenRefinementThrows()
    {
        var duration = new DurationAccumulator();
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var execution = new ThrowingRefiner().MeasureDuration(duration, timeProvider).CreateExecutionInstance<DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(ResolutionScope.Create());

        Should.Throw<InvalidOperationException>(() => Refine(execution, 3));

        duration.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void IteratedRefiner_WithOneIteration_MatchesTheBareRefiner()
    {
        var iterated = AddOffset(1).AsIterated(1).CreateExecutionInstance<DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(ResolutionScope.Create());

        Refine(iterated, 3, 4).ShouldBe(Refine(AddOffset(1).CreateExecutionInstance(ResolutionScope.Create()), 3, 4));
    }

    // Instrumentation reports what its own position sees, so the same counter says something different inside an
    // iterated refiner than around it.
    [Fact]
    public void ObservableRefiner_ReportsOncePerCallAtItsOwnPositionInTheComposition()
    {
        var inside = new CountAccumulator();
        var around = new CountAccumulator();
        var insideExecution = AddOffset(1).CountCalls(inside).AsIterated(3).CreateExecutionInstance<DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(ResolutionScope.Create());
        var aroundExecution = AddOffset(1).AsIterated(3).CountCalls(around).CreateExecutionInstance<DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(ResolutionScope.Create());

        Refine(insideExecution, 3).ShouldBe([6]);
        Refine(aroundExecution, 3).ShouldBe([6]);

        inside.CurrentCount.ShouldBe(3);
        around.CurrentCount.ShouldBe(1);
    }

    [Fact]
    public void NestedComposition_AppliesEveryStageAndItsInstrumentation()
    {
        var counter = new CountAccumulator();
        var execution = PipelineRefiner.Create(
                AddOffset(1).CountCandidates(counter),
                Multiply(2).AsIterated(2))
            .CreateExecutionInstance<DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(ResolutionScope.Create());

        Refine(execution, 3).ShouldBe([16]);
        counter.CurrentCount.ShouldBe(1);
    }

    [Fact]
    public void ChooseOneRefiner_WithTheSameSeed_MakesTheSameSelections()
    {
        var refiner = ChooseOneRefiner.Create([AddOffset(1), Multiply(2)], [1.0, 1.0]);
        var candidates = new[] { 3, 3, 3, 3, 3, 3, 3, 3 };

        var first = refiner.CreateExecutionInstance<DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(ResolutionScope.Create()).Refine(candidates, RandomNumberGenerator.Create(7), DummySearchSpace<int>.Instance, CreateProblem());
        var second = refiner.CreateExecutionInstance<DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(ResolutionScope.Create()).Refine(candidates, RandomNumberGenerator.Create(7), DummySearchSpace<int>.Instance, CreateProblem());

        second.ShouldBe(first);
        // Both children are actually reachable, or the comparison above would be vacuous.
        first.Distinct().Count().ShouldBe(2);
    }

    [Fact]
    public void WithRate_OfOne_RefinesEveryCandidate()
    {
        var execution = AddOffset(1).AppliedAtRate(1.0).CreateExecutionInstance<DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(ResolutionScope.Create());

        Refine(execution, 3, 4, 5).ShouldBe([4, 5, 6]);
    }

    private static AddOffsetRefiner AddOffset(int offset) => new(offset);

    private static MultiplyRefiner Multiply(int factor) => new(factor);

    private static IReadOnlyList<int> Refine(IRefinerExecution<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>> execution, params int[] candidates) =>
        execution.Refine(candidates, RandomNumberGenerator.Create(42), DummySearchSpace<int>.Instance, CreateProblem());

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
