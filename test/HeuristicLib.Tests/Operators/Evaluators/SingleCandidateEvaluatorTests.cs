using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Operators.Evaluators;

public class SingleCandidateEvaluatorTests
{
    [Fact]
    public void Concurrency_DefaultsToSequential()
    {
        new RandomEvaluator().Concurrency.ShouldBe(ExecutionConcurrency.Sequential());
    }

    [Fact]
    public void Evaluate_WithConcurrentBatching_ProducesSequentialResults()
    {
        var candidates = Enumerable.Range(0, 64).ToArray();
        var sequential = new RandomEvaluator().Evaluate(candidates, RandomNumberGenerator.Create(42), DummySearchSpace<int>.Instance);
        var concurrent = new RandomEvaluator { Concurrency = ExecutionConcurrency.Concurrent(4) }
            .Evaluate(candidates, RandomNumberGenerator.Create(42), DummySearchSpace<int>.Instance);

        concurrent.ShouldBe(sequential);
    }

    [Fact]
    public void Evaluate_WithEmptyBatch_ReturnsEmptyResult()
    {
        new RandomEvaluator { Concurrency = ExecutionConcurrency.Concurrent() }
            .Evaluate([], RandomNumberGenerator.Create(42), DummySearchSpace<int>.Instance)
            .ShouldBeEmpty();
    }

    [Fact]
    public void Evaluate_CallsEvaluateCandidateOncePerCandidate()
    {
        var counter = new ObservationCounter();
        var evaluator = new CountingEvaluator(counter);

        evaluator.Evaluate([1, 2, 3], RandomNumberGenerator.Create(42), DummySearchSpace<int>.Instance);

        counter.CurrentCount.ShouldBe(3);
    }

    private sealed record RandomEvaluator : SingleCandidateEvaluator<int, DummySearchSpace<int>>
    {
        public override ObjectiveVector EvaluateCandidate(int candidate, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace) =>
            new(random.NextInt(0, 1000));
    }

    private sealed record CountingEvaluator(ObservationCounter Counter) : SingleCandidateEvaluator<int, DummySearchSpace<int>>
    {
        public override ObjectiveVector EvaluateCandidate(int candidate, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace)
        {
            Counter.IncrementBy(1);
            return new(candidate);
        }
    }
}
