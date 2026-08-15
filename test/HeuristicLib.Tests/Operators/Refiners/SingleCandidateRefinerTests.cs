using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators.Refiners;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Operators.Refiners;

/// <summary>
/// Pins the batching contract of the single-candidate authoring base. Batching is a configuration decision rather than
/// an override, so the same candidates must produce the same refinement results for every concurrency setting.
/// </summary>
public class SingleCandidateRefinerTests
{
    [Fact]
    public void Concurrency_DefaultsToSequential()
    {
        new RandomOffsetRefiner().Concurrency.ShouldBe(ExecutionConcurrency.Sequential());
    }

    [Fact]
    public void Refine_WithConcurrentBatching_ProducesSequentialResults()
    {
        var candidates = Enumerable.Range(0, 64).ToArray();

        var sequential = new RandomOffsetRefiner().Refine(candidates, RandomNumberGenerator.Create(42), DummySearchSpace<int>.Instance);
        var concurrent = new RandomOffsetRefiner { Concurrency = ExecutionConcurrency.Concurrent(4) }
            .Refine(candidates, RandomNumberGenerator.Create(42), DummySearchSpace<int>.Instance);

        concurrent.ShouldBe(sequential);
    }

    [Fact]
    public void Refine_WithEmptyBatch_ReturnsEmptyResult()
    {
        var refiner = new RandomOffsetRefiner { Concurrency = ExecutionConcurrency.Concurrent() };

        refiner.Refine([], RandomNumberGenerator.Create(42), DummySearchSpace<int>.Instance).ShouldBeEmpty();
    }

    [Fact]
    public void Refine_CallsRefineCandidateOncePerCandidate()
    {
        var candidates = new[] { 10, 20, 30 };

        var refined = new RandomOffsetRefiner().Refine(candidates, RandomNumberGenerator.Create(42), DummySearchSpace<int>.Instance);

        refined.Count.ShouldBe(candidates.Length);
        refined.ShouldAllBe(candidate => candidate >= 10);
    }

    private sealed record RandomOffsetRefiner : SingleCandidateRefiner<int, DummySearchSpace<int>>
    {
        public override int RefineCandidate(int candidate, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace) =>
            candidate + random.NextInt(0, 1000);
    }
}
