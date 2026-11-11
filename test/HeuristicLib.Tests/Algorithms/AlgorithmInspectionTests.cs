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
        var completions = new Counter();
        var duration = new Duration(time);
        var observed = new List<IAlgorithm<int, PopulationState<int>>>();
        var scope = ResolutionScope.Create();
        var analyzed = new CountingCompletions(algorithm.AnalyzedBy(algorithm.Iterations.Observing(observation => observed.Add(observation.Algorithm))), completions);
        var problem = FuncProblem.Create(static (int candidate) => candidate, DummySearchSpace<int>.Instance, SingleObjective.Minimize);
        var executor = scope.For<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, PopulationState<int>>().Resolve(analyzed.Timed(duration));
        var count = scope.Results.Result(completions);
        var measured = scope.Results.Result(duration);
        await using var iterator = executor.RunStreamingAsync(problem, RandomNumberGenerator.Create(1), ct: TestContext.Current.CancellationToken)
            .GetAsyncEnumerator(TestContext.Current.CancellationToken);

        (await iterator.MoveNextAsync()).ShouldBeTrue();
        count.CurrentCount.ShouldBe(0);
        measured.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(2));
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
        measured.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(5));
        observed.Count.ShouldBe(fail ? 1 : 2);
        observed.ShouldAllBe(source => ReferenceEquals(source, algorithm));
    }

    [Fact]
    public void TimingTheIterations_PlacesTheTimedWrapper()
    {
        var algorithm = new ProbeAlgorithm(new ManualTimeProvider(), Fail: false);
        var time = algorithm.Iterations.Duration();

        algorithm.Timing(algorithm.Iterations, out var timed).ShouldBe(algorithm.Timed(timed));
        algorithm.AnalyzedBy(time).ShouldBe(algorithm.Timed(time));
    }

    private sealed record ProbeAlgorithm(ManualTimeProvider Time, bool Fail) : Algorithm<ProbeAlgorithm, int, PopulationState<int>>
    {
        protected override IAlgorithmExecutor<int, TSearchSpace, TProblem, PopulationState<int>> CreateExecutor<TSearchSpace, TProblem>(
            ResolutionScope<int, TSearchSpace, TProblem, PopulationState<int>> scope) =>
            new Executor<TSearchSpace, TProblem>(Time, Fail);

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

    // Counts the calls that finish enumerating their states.
    private sealed record CountingCompletions(IAlgorithm<int, PopulationState<int>> Child, Counter Completions)
        : WrappingAlgorithm<int, PopulationState<int>>(Child)
    {
        protected override IAlgorithmExecutor<int, TSearchSpace, TProblem, PopulationState<int>> CreateExecutor<TSearchSpace, TProblem>(
            ResolutionScope<int, TSearchSpace, TProblem, PopulationState<int>> scope)
        {
            var completions = scope.Resolve(Completions);
            var child = scope.Resolve(ChildAlgorithm);
            return new Executor<TSearchSpace, TProblem>(child, completions);
        }

        private sealed class Executor<TSearchSpace, TProblem>(
            IAlgorithmExecutor<int, TSearchSpace, TProblem, PopulationState<int>> child, CountAccumulator completions)
            : WrappingAlgorithmExecutor<int, TSearchSpace, TProblem, PopulationState<int>>(child)
            where TSearchSpace : class, ISearchSpace<int>
            where TProblem : class, IProblem<int, TSearchSpace>
        {
            public override async IAsyncEnumerable<PopulationState<int>> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random,
                PopulationState<int>? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
            {
                await foreach (var state in ChildAlgorithm.RunStreamingAsync(problem, random, initialState, ct))
                    yield return state;
                completions.IncrementBy(1);
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
