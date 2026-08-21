using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Operators.Mutators;

/// <summary>
/// Pins the batching contract of the single-candidate authoring base. Batching is a configuration decision rather than
/// an override, so the same parents must produce the same offspring for every concurrency setting.
/// </summary>
public class SingleCandidateMutatorTests
{
    [Fact]
    public void Concurrency_DefaultsToSequential()
    {
        new RandomOffsetMutator().Concurrency.ShouldBe(ExecutionConcurrency.Sequential());
    }

    [Fact]
    public void Mutate_WithConcurrentBatching_ProducesSequentialResults()
    {
        var parents = Enumerable.Range(0, 64).ToArray();

        var sequential = new RandomOffsetMutator().Mutate(parents, RandomNumberGenerator.Create(42), DummySearchSpace<int>.Instance);
        var concurrent = new RandomOffsetMutator { Concurrency = ExecutionConcurrency.Concurrent(4) }
            .Mutate(parents, RandomNumberGenerator.Create(42), DummySearchSpace<int>.Instance);

        concurrent.ShouldBe(sequential);
    }

    [Fact]
    public void Mutate_WithEmptyBatch_ReturnsEmptyResult()
    {
        var mutator = new RandomOffsetMutator { Concurrency = ExecutionConcurrency.Concurrent() };

        mutator.Mutate([], RandomNumberGenerator.Create(42), DummySearchSpace<int>.Instance).ShouldBeEmpty();
    }

    [Fact]
    public void Mutate_CallsMutateCandidateOncePerParent()
    {
        var parents = new[] { 10, 20, 30 };

        var offspring = new RandomOffsetMutator().Mutate(parents, RandomNumberGenerator.Create(42), DummySearchSpace<int>.Instance);

        offspring.Count.ShouldBe(parents.Length);
        offspring.ShouldAllBe(candidate => candidate >= 10);
    }

    private sealed record RandomOffsetMutator : SingleCandidateMutator<int, DummySearchSpace<int>>
    {
        public override int MutateCandidate(int parent, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace) =>
            parent + random.NextInt(0, 1000);
    }
}
