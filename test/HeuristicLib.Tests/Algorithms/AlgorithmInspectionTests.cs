using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Algorithms;

public sealed class AlgorithmInspectionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CountsSuccessfulCompletionAndTimesIterationWorkIncludingFailures(bool fail)
    {
        var time = new ManualTimeProvider();
        var algorithm = new ProbeAlgorithm(time, fail);
        var count = new CountAccumulator();
        var duration = new DurationAccumulator();
        var observed = new List<IAlgorithm<int, PopulationState<int>>>();
        var scope = ResolutionScope.Create(Aspect.CountCalls(algorithm, count),
            Aspect.MeasureDuration(algorithm, duration, time),
            Aspect.Observe(algorithm.Iterations, observation => observed.Add(observation.Algorithm)));
        var problem = FuncProblem.Create(static (int candidate) => candidate, DummySearchSpace<int>.Instance, SingleObjective.Minimize);
        var execution = scope.For<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, PopulationState<int>>().Resolve(algorithm);
        await using var iterator = execution.RunStreamingAsync(problem, RandomNumberGenerator.Create(1), ct: TestContext.Current.CancellationToken)
            .GetAsyncEnumerator(TestContext.Current.CancellationToken);

        (await iterator.MoveNextAsync()).ShouldBeTrue();
        count.CurrentCount.ShouldBe(0);
        duration.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(2));
        time.Advance(20);
        if (fail)
            await Should.ThrowAsync<InvalidOperationException>(async () => await iterator.MoveNextAsync());
        else
        {
            (await iterator.MoveNextAsync()).ShouldBeTrue();
            count.CurrentCount.ShouldBe(0);
            (await iterator.MoveNextAsync()).ShouldBeFalse();
        }

        count.CurrentCount.ShouldBe(fail ? 0 : 1);
        duration.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(5));
        observed.Count.ShouldBe(fail ? 1 : 2);
        observed.ShouldAllBe(source => ReferenceEquals(source, algorithm));
    }

    private sealed record ProbeAlgorithm(ManualTimeProvider Time, bool Fail) : Algorithm<ProbeAlgorithm, int, PopulationState<int>>
    {
        public override ExecutorFactory<IAlgorithmExecutor<int, TRunSearchSpace, TRunProblem, PopulationState<int>>> CreateExecutorFactory<TRunSearchSpace, TRunProblem>() =>
            _ => new Executor<TRunSearchSpace, TRunProblem>(Time, Fail);

        private sealed class Executor<TSearchSpace, TProblem>(ManualTimeProvider time, bool fail)
            : AlgorithmExecutor<int, TSearchSpace, TProblem, PopulationState<int>>
            where TSearchSpace : class, ISearchSpace<int>
            where TProblem : class, IProblem<int, TSearchSpace>
        {
            public override async IAsyncEnumerable<PopulationState<int>> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random,
                PopulationState<int>? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
            {
                time.Advance(2);
                yield return Population.From([1.ToEvaluated(1)]).ToPopulationState();
                await Task.CompletedTask;
                time.Advance(3);
                if (fail)
                    throw new InvalidOperationException("Iteration failed.");
                yield return Population.From([2.ToEvaluated(2)]).ToPopulationState();
            }
        }
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private long timestamp;
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => timestamp;
        public void Advance(int seconds) => timestamp += TimeSpan.FromSeconds(seconds).Ticks;
    }
}
