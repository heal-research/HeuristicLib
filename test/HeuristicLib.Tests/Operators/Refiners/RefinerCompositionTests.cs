using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators.Refiners;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Operators.Refiners;

public class RefinerCompositionTests
{
    [Fact]
    public void PipelineRefiner_AppliesChildRefinersInConfiguredOrder()
    {
        var addThenDouble = ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(PipelineRefiner.Create(AddOffset(1), Multiply(2)), CreateProblem());
        var doubleThenAdd = ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(PipelineRefiner.Create(Multiply(2), AddOffset(1)), CreateProblem());

        Refine(addThenDouble, 3).ShouldBe([8]);
        Refine(doubleThenAdd, 3).ShouldBe([7]);
    }

    [Fact]
    public void PipelineRefiner_AppliesARepeatedStageEveryTimeItAppears()
    {
        var simplify = AddOffset(1);
        var executor = ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(PipelineRefiner.Create(simplify, Multiply(2), simplify), CreateProblem());

        Refine(executor, 3).ShouldBe([9]);
    }

    [Fact]
    public void PipelineRefiner_WithoutChildRefiners_ReturnsCandidatesUnchanged()
    {
        var executor = ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(PipelineRefiner.Create<int>(), CreateProblem());

        Refine(executor, 3, 4).ShouldBe([3, 4]);
    }

    [Fact]
    public void IteratedRefiner_AppliesTheChildRefinerOncePerIteration()
    {
        var observed = AddOffset(1);
        var scope = ResolutionScope.Create();
        var executor = scope.Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(observed.AsIterated(4).Counting(observed, out var counter), CreateProblem());

        Refine(executor, 3).ShouldBe([7]);
        scope.Results.Result(counter).CurrentCount.ShouldBe(4);
    }

    [Fact]
    public void IteratedRefiner_WithNonPositiveIterations_ThrowsWhenCreatingTheExecution()
    {
        var refiner = AddOffset(1).AsIterated(0);

        Should.Throw<InvalidOperationException>(() => ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(refiner, CreateProblem()));
    }

    [Fact]
    public void IteratedRefiner_RetainsItsChildRefinerAndIterations()
    {
        var child = AddOffset(1);
        var refiner = child.AsIterated(3);

        refiner.ChildOperator.ShouldBeSameAs(child);
        refiner.Iterations.ShouldBe(3);
    }

    [Fact]
    public void ChoosePerItemRefinement_WithoutChildRefiners_ThrowsWhenCreatingTheExecution()
    {
        var refiner = ChoosePerItem.Create<Refinement, int>();

        Should.Throw<InvalidOperationException>(() => ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(refiner, CreateProblem()));
    }

    [Fact]
    public void ChoosePerItemRefinement_WithMismatchedWeightCount_ThrowsWhenCreatingTheExecution()
    {
        var refiner = ChoosePerItem.Create([AddOffset(1), Multiply(2)], [1.0]);

        Should.Throw<InvalidOperationException>(() => ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(refiner, CreateProblem()));
    }

    [Fact]
    public void ChoosePerItemRefinement_WithZeroWeightForAChild_NeverSelectsThatChild()
    {
        var executor = ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(ChoosePerItem.Create([AddOffset(1), Multiply(2)], [1.0, 0.0]), CreateProblem());

        Refine(executor, 3, 3, 3, 3).ShouldBe([4, 4, 4, 4]);
    }

    [Fact]
    public void WithRate_OfZero_LeavesEveryCandidateUnchanged()
    {
        var executor = ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(AddOffset(1).AppliedAtRate(0.0), CreateProblem());

        Refine(executor, 3, 4, 5).ShouldBe([3, 4, 5]);
    }

    [Fact]
    public void NoChangeRefiner_ReturnsTheSuppliedCandidates()
    {
        NoChangeRefiner<int>.Instance.Refine([3, 4, 5], RandomNumberGenerator.Create(1)).ShouldBe([3, 4, 5]);
    }

    [Fact]
    public void CountRefinedCandidates_CountsEveryReturnedCandidate()
    {
        var counter = new Counter();
        var observed = AddOffset(1);
        var scope = ResolutionScope.Create();
        var executor = scope.Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(observed.Counted(counter, OperatorCountMetric.Candidates), CreateProblem());

        Refine(executor, 3, 4, 5);

        scope.Results.Result(counter).CurrentCount.ShouldBe(3);
    }

    [Fact]
    public void CountRefinerCalls_DoesNotIncrementWhenRefinementThrows()
    {
        var counter = new Counter();
        var observed = new ThrowingRefiner();
        var scope = ResolutionScope.Create();
        var executor = scope.Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(observed.Counted(counter), CreateProblem());

        Should.Throw<InvalidOperationException>(() => Refine(executor, 3));

        scope.Results.Result(counter).CurrentCount.ShouldBe(0);
    }

    [Fact]
    public void MeasureRefinerDuration_RecordsElapsedDurationWhenRefinementThrows()
    {
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var duration = new Duration();
        var observed = new ThrowingRefiner();
        var scope = ResolutionScope.Create(timeProvider);
        var executor = scope.Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(observed.Timed(duration), CreateProblem());

        Should.Throw<InvalidOperationException>(() => Refine(executor, 3));

        scope.Results.Result(duration).CurrentDuration.ShouldBe(TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void IteratedRefiner_WithOneIteration_MatchesTheBareRefiner()
    {
        var iterated = ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(AddOffset(1).AsIterated(1), CreateProblem());

        Refine(iterated, 3, 4).ShouldBe(Refine(ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(AddOffset(1), CreateProblem()), 3, 4));
    }

    // Inspection reports what its own position sees, so the same counter says something different inside an
    // iterated refiner than around it.
    [Fact]
    public void ObservableRefiner_ReportsOncePerCallAtItsOwnPositionInTheComposition()
    {
        var around = new Counter();
        var inner = AddOffset(1);
        var outer = AddOffset(1).AsIterated(3);
        var insideScope = ResolutionScope.Create();
        var insideExecutor = insideScope.Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(inner.AsIterated(3).Counting(inner, out var inside), CreateProblem());
        var aroundScope = ResolutionScope.Create();
        var aroundExecutor = aroundScope.Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(outer.Counted(around), CreateProblem());

        Refine(insideExecutor, 3).ShouldBe([6]);
        Refine(aroundExecutor, 3).ShouldBe([6]);

        insideScope.Results.Result(inside).CurrentCount.ShouldBe(3);
        aroundScope.Results.Result(around).CurrentCount.ShouldBe(1);
    }

    [Fact]
    public void NestedComposition_AppliesEveryStageAndItsInspection()
    {
        var observed = AddOffset(1);
        var refiner = PipelineRefiner.Create(observed, Multiply(2).AsIterated(2));
        var scope = ResolutionScope.Create();
        var executor = scope.Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(refiner.Counting(observed, out var counter, OperatorCountMetric.Candidates), CreateProblem());

        Refine(executor, 3).ShouldBe([16]);
        scope.Results.Result(counter).CurrentCount.ShouldBe(1);
    }

    [Fact]
    public void ChoosePerItemRefinement_WithTheSameSeed_MakesTheSameSelections()
    {
        var refiner = ChoosePerItem.Create([AddOffset(1), Multiply(2)], [1.0, 1.0]);
        var candidates = new[] { 3, 3, 3, 3, 3, 3, 3, 3 };

        var first = ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(refiner, CreateProblem()).Refine(candidates, RandomNumberGenerator.Create(7));
        var second = ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(refiner, CreateProblem()).Refine(candidates, RandomNumberGenerator.Create(7));

        second.ShouldBe(first);
        // Both children are actually reachable, or the comparison above would be vacuous.
        first.Distinct().Count().ShouldBe(2);
    }

    [Fact]
    public void WithRate_OfOne_RefinesEveryCandidate()
    {
        var executor = ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(AddOffset(1).AppliedAtRate(1.0), CreateProblem());

        Refine(executor, 3, 4, 5).ShouldBe([4, 5, 6]);
    }

    private static AddOffsetRefiner AddOffset(int offset) => new(offset);

    private static MultiplyRefiner Multiply(int factor) => new(factor);

    private static IReadOnlyList<int> Refine(IRefinementExecutor<int> executor, params int[] candidates) =>
        executor.Refine(candidates, RandomNumberGenerator.Create(42));

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
