using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Operators.Evaluators;

public class ObservableEvaluatorTests
{
    [Fact]
    public void CountEvaluatorCalls_IncrementsOncePerEvaluateCall()
    {
        var counter = new ObservationCounter();
        var evaluator = CreateEvaluator().CountEvaluatorCalls(counter);
        evaluator.Counter.ShouldBeSameAs(counter);
        evaluator.Metric.ShouldBe(OperatorCountMetric.Calls);
        var instance = new ExecutionInstanceRegistry().Resolve(evaluator);
        var problem = CreateProblem();

        instance.Evaluate([1, 2, 3], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        instance.Evaluate([4], RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        counter.CurrentCount.ShouldBe(2);
    }

    [Fact]
    public void CountEvaluatedCandidates_IncrementsByBatchSize()
    {
        var counter = new ObservationCounter();
        var evaluator = CreateEvaluator().CountEvaluatedCandidates(counter);
        var instance = new ExecutionInstanceRegistry().Resolve(evaluator);
        var problem = CreateProblem();

        instance.Evaluate([1, 2, 3], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        instance.Evaluate([4], RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        counter.CurrentCount.ShouldBe(4);
    }

    [Fact]
    public void MeasureEvaluatorDuration_AddsElapsedEvaluatorExecutionDuration()
    {
        var duration = new ObservationDuration();
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var evaluator = CreateEvaluator().MeasureEvaluatorDuration(duration, timeProvider);
        evaluator.Duration.ShouldBeSameAs(duration);
        evaluator.TimeProvider.ShouldBeSameAs(timeProvider);
        var instance = new ExecutionInstanceRegistry().Resolve(evaluator);
        var problem = CreateProblem();

        instance.Evaluate([1, 2, 3], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        instance.Evaluate([4], RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        duration.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(6));
    }

    [Fact]
    public void CountEvaluatorCalls_DoesNotCountFailedCall()
    {
        var counter = new ObservationCounter();
        var evaluator = new ThrowingEvaluator().CountEvaluatorCalls(counter);
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() =>
            evaluator.CreateExecutionInstance().Evaluate([1], RandomNumberGenerator.Create(1), problem.SearchSpace, problem));

        counter.CurrentCount.ShouldBe(0);
    }

    [Fact]
    public void MeasureEvaluatorDuration_RecordsFailedCall()
    {
        var duration = new ObservationDuration();
        var evaluator = new ThrowingEvaluator().MeasureEvaluatorDuration(duration, new AdvancingTimeProvider(TimeSpan.FromSeconds(3)));
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() =>
            evaluator.CreateExecutionInstance().Evaluate([1], RandomNumberGenerator.Create(1), problem.SearchSpace, problem));

        duration.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void ObservationDuration_AllowsNegativeAdjustments()
    {
        var duration = new ObservationDuration();

        duration.AddDuration(TimeSpan.FromSeconds(5));
        duration.AddDuration(TimeSpan.FromSeconds(-2));

        duration.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(3));
    }

    private static IEvaluator<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> CreateEvaluator()
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
