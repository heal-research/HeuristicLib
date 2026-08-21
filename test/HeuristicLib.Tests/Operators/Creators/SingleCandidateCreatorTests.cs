using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Operators.Creators;

/// <summary>
/// Pins the batching contract of the single-candidate authoring base. Batching is a configuration decision rather than
/// an override, so the same requested count must produce the same candidates for every concurrency setting.
/// </summary>
public class SingleCandidateCreatorTests
{
    [Fact]
    public void Concurrency_DefaultsToSequential()
    {
        new RandomValueCreator().Concurrency.ShouldBe(ExecutionConcurrency.Sequential());
    }

    [Fact]
    public void Create_WithConcurrentBatching_ProducesSequentialResults()
    {
        var sequential = new RandomValueCreator().Create(64, RandomNumberGenerator.Create(42), DummySearchSpace<int>.Instance);
        var concurrent = new RandomValueCreator { Concurrency = ExecutionConcurrency.Concurrent(4) }
            .Create(64, RandomNumberGenerator.Create(42), DummySearchSpace<int>.Instance);

        concurrent.ShouldBe(sequential);
    }

    [Fact]
    public void Create_WithZeroCount_ReturnsEmptyResult()
    {
        var creator = new RandomValueCreator { Concurrency = ExecutionConcurrency.Concurrent() };

        creator.Create(0, RandomNumberGenerator.Create(42), DummySearchSpace<int>.Instance).ShouldBeEmpty();
    }

    [Fact]
    public void Create_CallsCreateCandidateOncePerRequestedCandidate()
    {
        var candidates = new RandomValueCreator().Create(3, RandomNumberGenerator.Create(42), DummySearchSpace<int>.Instance);

        candidates.Count.ShouldBe(3);
        candidates.ShouldAllBe(candidate => candidate >= 0 && candidate < 1000);
    }

    private sealed record RandomValueCreator : SingleCandidateCreator<int, DummySearchSpace<int>>
    {
        public override int CreateCandidate(IRandomNumberGenerator random, DummySearchSpace<int> searchSpace) =>
            random.NextInt(0, 1000);
    }
}
