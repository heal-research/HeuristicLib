using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Analysis;

public sealed class AccumulatorConcurrencyTests
{
    private const int WriterCount = 8;
    private const int AdditionsPerWriter = 100_000;

    [Theory]
    [InlineData(3, 3)]
    [InlineData(3, -2)]
    public async Task CountAccumulator_ConcurrentSignedAdditionsKeepTheExactTotal(long evenAmount, long oddAmount)
    {
        var accumulator = new CountAccumulator();

        await RunWriters(writer =>
        {
            var amount = writer % 2 == 0 ? evenAmount : oddAmount;
            return () => accumulator.IncrementBy(amount);
        });

        accumulator.CurrentCount.ShouldBe((WriterCount / 2) * (evenAmount + oddAmount) * AdditionsPerWriter);
    }

    [Fact]
    public async Task DurationAccumulator_ConcurrentAdditionsKeepTheExactTotal()
    {
        var accumulator = new DurationAccumulator();
        var amount = TimeSpan.FromTicks(7);

        await RunWriters(_ => () => accumulator.AddDuration(amount));

        accumulator.CurrentDuration.ShouldBe(TimeSpan.FromTicks(WriterCount * AdditionsPerWriter * amount.Ticks));
    }

    [Theory]
    [InlineData(OperatorCountMetric.Invocations, 1)]
    [InlineData(OperatorCountMetric.Candidates, 3)]
    public async Task Counted_ConcurrentEvaluationsKeepTheExactTotal(OperatorCountMetric metric, int countPerCall)
    {
        var counter = new Counter();
        var evaluator = new DummyEvaluator<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>();
        var scope = ResolutionScope.Create();
        var executor = scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(
            evaluator.Counted(counter, metric), CreateProblem());
        int[] candidates = [1, 2, 3];

        await RunWriters(writer =>
        {
            var random = RandomNumberGenerator.Create(writer);
            return () => executor.Evaluate(candidates, random);
        });

        scope.Results.Result(counter).CurrentCount.ShouldBe((long)WriterCount * AdditionsPerWriter * countPerCall);
    }

    [Fact]
    public async Task Timed_ConcurrentEvaluationsKeepTheExactTotal()
    {
        var elapsedPerCall = TimeSpan.FromTicks(7);
        using var timeProvider = new PerThreadAdvancingTimeProvider(elapsedPerCall);
        var duration = new Duration();
        var evaluator = new DummyEvaluator<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>();
        var scope = ResolutionScope.Create(timeProvider);
        var executor = scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(
            evaluator.Timed(duration), CreateProblem());
        int[] candidates = [1, 2, 3];

        await RunWriters(writer =>
        {
            var random = RandomNumberGenerator.Create(writer);
            return () => executor.Evaluate(candidates, random);
        });

        scope.Results.Result(duration).CurrentDuration.ShouldBe(
            TimeSpan.FromTicks(WriterCount * AdditionsPerWriter * elapsedPerCall.Ticks));
    }

    private static async Task RunWriters(Func<int, Action> createAddition)
    {
        using var start = new Barrier(WriterCount);
        var cancellationToken = TestContext.Current.CancellationToken;
        var writers = Enumerable.Range(0, WriterCount).Select(writer => Task.Factory.StartNew(() =>
        {
            var add = createAddition(writer);
            // Rendezvous again in each batch so short additions still contend across all eight writers.
            const int batches = 10;
            for (var batch = 0; batch < batches; batch++)
            {
                start.SignalAndWait(TimeSpan.FromSeconds(10), cancellationToken).ShouldBeTrue();
                for (var addition = 0; addition < AdditionsPerWriter / batches; addition++)
                    add();
            }
        }, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray();

        await Task.WhenAll(writers).WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
    }

    private static FuncProblem<int, DummySearchSpace<int>> CreateProblem() => FuncProblem.Create(
        evaluateFunc: static (int candidate) => candidate,
        encoding: DummySearchSpace<int>.Instance,
        objective: new ObjectiveDirections(
            [ObjectiveDirection.Minimize],
            Comparer<ObjectiveVector>.Create(static (left, right) => left[0].CompareTo(right[0]))));

    private sealed class PerThreadAdvancingTimeProvider(TimeSpan step) : TimeProvider, IDisposable
    {
        private readonly ThreadLocal<long> timestamp = new();

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp()
        {
            // Each synchronous evaluation reads twice on its writer, independent of other writers' calls.
            var current = timestamp.Value;
            timestamp.Value = current + step.Ticks;
            return current;
        }

        public void Dispose()
        {
            timestamp.Dispose();
        }
    }
}
