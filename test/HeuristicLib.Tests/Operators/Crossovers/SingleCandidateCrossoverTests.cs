using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Operators.Crossovers;

/// <summary>
/// Pins the batching contract of the single-candidate authoring base. Batching is a configuration decision rather than
/// an override, so the same parent groups must produce the same offspring for every concurrency setting.
/// </summary>
public class SingleCandidateCrossoverTests
{
    [Fact]
    public void Concurrency_DefaultsToSequential()
    {
        new RandomBlendCrossover().Concurrency.ShouldBe(ExecutionConcurrency.Sequential());
    }

    [Fact]
    public void Cross_WithConcurrentBatching_ProducesSequentialResults()
    {
        var parents = Enumerable.Range(0, 64).Select(i => new Parents<int>(i, i + 1000)).ToArray();

        var sequential = new RandomBlendCrossover().Cross(parents, RandomNumberGenerator.Create(42), DummySearchSpace<int>.Instance);
        var concurrent = new RandomBlendCrossover { Concurrency = ExecutionConcurrency.Concurrent(4) }
            .Cross(parents, RandomNumberGenerator.Create(42), DummySearchSpace<int>.Instance);

        concurrent.ShouldBe(sequential);
    }

    [Fact]
    public void Cross_WithEmptyBatch_ReturnsEmptyResult()
    {
        var crossover = new RandomBlendCrossover { Concurrency = ExecutionConcurrency.Concurrent() };

        crossover.Cross([], RandomNumberGenerator.Create(42), DummySearchSpace<int>.Instance).ShouldBeEmpty();
    }

    [Fact]
    public void Cross_CallsCrossParentsOncePerParentGroup()
    {
        Parents<int>[] parents = [new(10, 20), new(30, 40), new(50, 60)];

        var offspring = new RandomBlendCrossover().Cross(parents, RandomNumberGenerator.Create(42), DummySearchSpace<int>.Instance);

        offspring.Count.ShouldBe(parents.Length);
        offspring.ShouldAllBe(candidate => candidate >= 10);
    }

    private sealed record RandomBlendCrossover : SingleCandidateCrossover<int, DummySearchSpace<int>>
    {
        public override int CrossParents(Parents<int> parents, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace) =>
            random.NextDouble() < 0.5 ? parents.Parent1 : parents.Parent2;
    }
}
