using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Operators.Evaluators;

public class EvaluatorInspectionTests
{
    [Fact]
    public void CountEvaluatorCalls_IncrementsOncePerEvaluateCall()
    {
        var counter = new Counter();
        var evaluator = CreateEvaluator();
        var counted = evaluator.Counted(counter);
        counted.Counter.ShouldBeSameAs(counter);
        counted.Metric.ShouldBe(OperatorCountMetric.Invocations);
        var scope = ResolutionScope.Create();
        var problem = CreateProblem();
        var executor = scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(counted, problem);

        executor.Evaluate([1, 2, 3], RandomNumberGenerator.Create(1));
        executor.Evaluate([4], RandomNumberGenerator.Create(2));

        scope.Results.Result(counter).CurrentCount.ShouldBe(2);
    }

    [Fact]
    public void CountEvaluatedCandidates_IncrementsByBatchSize()
    {
        var counter = new Counter();
        var evaluator = CreateEvaluator();
        var scope = ResolutionScope.Create();
        var problem = CreateProblem();
        var executor = scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(evaluator.Counted(counter, OperatorCountMetric.Candidates), problem);

        executor.Evaluate([1, 2, 3], RandomNumberGenerator.Create(1));
        executor.Evaluate([4], RandomNumberGenerator.Create(2));

        scope.Results.Result(counter).CurrentCount.ShouldBe(4);
    }

    [Fact]
    public void MeasureEvaluatorDuration_AddsElapsedEvaluatorExecutionDuration()
    {
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var duration = new Duration();
        var evaluator = CreateEvaluator();
        var scope = ResolutionScope.Create(timeProvider);
        var problem = CreateProblem();
        var executor = scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(evaluator.Timed(duration), problem);

        executor.Evaluate([1, 2, 3], RandomNumberGenerator.Create(1));
        executor.Evaluate([4], RandomNumberGenerator.Create(2));

        scope.Results.Result(duration).CurrentDuration.ShouldBe(TimeSpan.FromSeconds(6));
    }

    [Fact]
    public void CountEvaluatorCalls_DoesNotCountFailedCall()
    {
        var counter = new Counter();
        var evaluator = new ThrowingEvaluator();
        var problem = CreateProblem();

        var scope = ResolutionScope.Create();
        Should.Throw<InvalidOperationException>(() =>
            scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(evaluator.Counted(counter), problem).Evaluate([1], RandomNumberGenerator.Create(1)));

        scope.Results.Result(counter).CurrentCount.ShouldBe(0);
    }

    [Fact]
    public void MeasureEvaluatorDuration_RecordsFailedCall()
    {
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var duration = new Duration();
        var evaluator = new ThrowingEvaluator();
        var problem = CreateProblem();

        var scope = ResolutionScope.Create(timeProvider);
        Should.Throw<InvalidOperationException>(() =>
            scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(evaluator.Timed(duration), problem).Evaluate([1], RandomNumberGenerator.Create(1)));

        scope.Results.Result(duration).CurrentDuration.ShouldBe(TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void DurationAccumulator_AllowsNegativeAdjustments()
    {
        var duration = new DurationAccumulator();

        duration.AddDuration(TimeSpan.FromSeconds(5));
        duration.AddDuration(TimeSpan.FromSeconds(-2));

        duration.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(3));
    }

    private static IOperator<Evaluation, int> CreateEvaluator()
    {
        return new DummyEvaluator<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>();
    }

    private static FuncProblem<int, DummySearchSpace<int>> CreateProblem()
    {
        return FuncProblem.Create(
            evaluateFunc: static (int candidate) => candidate,
            encoding: DummySearchSpace<int>.Instance,
            objective: CreateObjective());
    }

    private static ObjectiveDirections CreateObjective()
    {
        return new ObjectiveDirections(
            [ObjectiveDirection.Minimize],
            Comparer<ObjectiveVector>.Create(static (left, right) => left[0].CompareTo(right[0])));
    }

    private sealed class AdvancingTimeProvider(TimeSpan step) : TimeProvider
    {
        private long timestamp;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp()
        {
            var current = timestamp;
            timestamp += step.Ticks;
            return current;
        }
    }

    private sealed record ThrowingEvaluator : StatelessEvaluator<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>
    {
        public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<int> candidates, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace, FuncProblem<int, DummySearchSpace<int>> problem) =>
            throw new InvalidOperationException();
    }
}
