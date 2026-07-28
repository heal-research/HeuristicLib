using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Tests.ExecutionModel;

public class ExecutionConcurrencyTests
{
    [Fact]
    public void Sequential_IsItsOwnCategory()
    {
        var concurrency = ExecutionConcurrency.Sequential();

        concurrency.Kind.ShouldBe(ExecutionConcurrencyKind.Sequential);
        concurrency.MaximumConcurrency.ShouldBe(1);
    }

    [Fact]
    public void ConcurrentWithoutLimit_UsesTheConcurrentCategory()
    {
        var concurrency = ExecutionConcurrency.Concurrent();

        concurrency.Kind.ShouldBe(ExecutionConcurrencyKind.Concurrent);
        concurrency.MaximumConcurrency.ShouldBe(int.MaxValue);
    }

    [Fact]
    public void ConcurrentWithOne_RemainsCategoricallyConcurrent()
    {
        var concurrency = ExecutionConcurrency.Concurrent(1);

        concurrency.Kind.ShouldBe(ExecutionConcurrencyKind.Concurrent);
        concurrency.MaximumConcurrency.ShouldBe(1);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Concurrent_RequiresPositiveMaximumConcurrency(int maximumConcurrency)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => ExecutionConcurrency.Concurrent(maximumConcurrency));
    }
}
