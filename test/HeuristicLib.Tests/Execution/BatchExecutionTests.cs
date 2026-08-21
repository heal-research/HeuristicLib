namespace HEAL.HeuristicLib.Tests.ExecutionModel;

public class BatchExecutionTests
{
    [Fact]
    public void Sequential_ProcessesInputsInOrder()
    {
        var processingOrder = new List<int>();

        var results = BatchExecution.Sequential([3, 1, 2], (value, _) =>
        {
            processingOrder.Add(value);
            return value * 2;
        }, RandomNumberGenerator.Create(42));

        processingOrder.ShouldBe([3, 1, 2]);
        results.ShouldBe([6, 2, 4]);
    }

    [Fact]
    public void Concurrent_PreservesOutputOrder()
    {
        var results = BatchExecution.Parallel([3, 1, 2], (value, _) => value * 2, RandomNumberGenerator.Create(42));

        results.ShouldBe([6, 2, 4]);
    }

    [Fact]
    public void Concurrent_RespectsMaximumConcurrency()
    {
        var active = 0;
        var maximumActive = 0;
        var started = 0;
        using var firstTwoStarted = new CountdownEvent(2);

        _ = BatchExecution.Parallel(20, _ =>
        {
            var currentActive = Interlocked.Increment(ref active);
            UpdateMaximum(ref maximumActive, currentActive);

            if (Interlocked.Increment(ref started) <= 2)
            {
                firstTwoStarted.Signal();
                firstTwoStarted.Wait(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);
            }

            Interlocked.Decrement(ref active);
            return currentActive;
        }, RandomNumberGenerator.Create(42), maximumConcurrency: 2);

        maximumActive.ShouldBe(2);
    }

    [Fact]
    public void RandomForks_DoNotDependOnConcurrency()
    {
        var sequential = BatchExecution.Sequential(20, random => random.NextInt(), RandomNumberGenerator.Create(42));
        var concurrent = BatchExecution.Parallel(20, random => random.NextInt(), RandomNumberGenerator.Create(42), maximumConcurrency: 4);

        concurrent.ShouldBe(sequential);
    }

    [Fact]
    public void CountConvenienceMethods_PreserveIndexedResults()
    {
        var sequential = BatchExecution.Sequential(3, random => random.NextInt(), RandomNumberGenerator.Create(42));
        var parallel = BatchExecution.Parallel(3, random => random.NextInt(), RandomNumberGenerator.Create(42));

        parallel.ShouldBe(sequential);
    }

    [Fact]
    public void StateCarryingOverloads_PreserveOutputOrder()
    {
        var sequential = BatchExecution.Sequential([3, 1, 2], 10, (value, _, offset) => value + offset, RandomNumberGenerator.Create(42));
        var parallel = BatchExecution.Parallel([3, 1, 2], 10, (value, _, offset) => value + offset, RandomNumberGenerator.Create(42));

        sequential.ShouldBe([13, 11, 12]);
        parallel.ShouldBe(sequential);
    }

    [Fact]
    public void StateCarryingRandomForks_DoNotDependOnConcurrency()
    {
        var inputs = Enumerable.Range(0, 20).ToArray();

        var sequential = BatchExecution.Execute(inputs, 3, (value, random, factor) => value + (random.NextInt() * factor), RandomNumberGenerator.Create(42), ExecutionConcurrency.Sequential());
        var concurrent = BatchExecution.Execute(inputs, 3, (value, random, factor) => value + (random.NextInt() * factor), RandomNumberGenerator.Create(42), ExecutionConcurrency.Concurrent(4));

        concurrent.ShouldBe(sequential);
    }

    [Fact]
    public void EmptyInputs_ProduceEmptyOutputs()
    {
        Func<IRandomNumberGenerator, int> countOperation = _ => throw new InvalidOperationException();
        Func<int, IRandomNumberGenerator, int> listOperation = (_, _) => throw new InvalidOperationException();

        BatchExecution.Sequential(0, countOperation, RandomNumberGenerator.Create(42)).ShouldBeEmpty();
        BatchExecution.Parallel(0, countOperation, RandomNumberGenerator.Create(42)).ShouldBeEmpty();
        BatchExecution.Sequential(Array.Empty<int>(), listOperation, RandomNumberGenerator.Create(42)).ShouldBeEmpty();
        BatchExecution.Parallel(Array.Empty<int>(), listOperation, RandomNumberGenerator.Create(42)).ShouldBeEmpty();

        Func<int, IRandomNumberGenerator, int, int> statefulOperation = (_, _, _) => throw new InvalidOperationException();

        BatchExecution.Sequential(Array.Empty<int>(), 0, statefulOperation, RandomNumberGenerator.Create(42)).ShouldBeEmpty();
        BatchExecution.Parallel(Array.Empty<int>(), 0, statefulOperation, RandomNumberGenerator.Create(42)).ShouldBeEmpty();
    }

    [Fact]
    public void Parallel_FollowsTplExceptionBehavior()
    {
        var exception = Should.Throw<AggregateException>(() =>
            BatchExecution.Parallel<int>(1, _ => throw new InvalidOperationException("Failure"), RandomNumberGenerator.Create(42)));

        exception.InnerExceptions.ShouldHaveSingleItem().ShouldBeOfType<InvalidOperationException>();
    }

    private static void UpdateMaximum(ref int maximum, int candidate)
    {
        var currentMaximum = Volatile.Read(ref maximum);
        while (candidate > currentMaximum)
        {
            var observed = Interlocked.CompareExchange(ref maximum, candidate, currentMaximum);
            if (observed == currentMaximum)
            {
                return;
            }

            currentMaximum = observed;
        }
    }
}
