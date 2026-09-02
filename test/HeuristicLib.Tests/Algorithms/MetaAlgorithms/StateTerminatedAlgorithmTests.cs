using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Algorithms.MetaAlgorithms;

public class StateTerminatedAlgorithmTests
{
    [Fact]
    public void Stream_DoesNotCheckSuppliedInitialState()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var terminator = new RecordingTerminator(_ => false);
        var algorithm = CreateStateTerminatedAlgorithm(terminator);
        var initialState = CreateState(41);

        var states = algorithm.Stream(problem, RandomNumberGenerator.Create(42), initialState, TestContext.Current.CancellationToken).ToList();

        states.Select(MetaAlgorithmTestHelpers.StateCandidate).ShouldBe([42]);
        terminator.CheckedCandidates.ShouldBe([42]);
    }

    [Fact]
    public void Stream_YieldsFirstProducedStateBeforeCheckingTerminator()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var terminator = new RecordingTerminator(_ => true);
        var algorithm = CreateStateTerminatedAlgorithm(terminator);
        var initialState = CreateState(41);

        var states = algorithm.Stream(problem, RandomNumberGenerator.Create(42), initialState, TestContext.Current.CancellationToken).ToList();

        states.Select(MetaAlgorithmTestHelpers.StateCandidate).ShouldBe([42]);
        terminator.CheckedCandidates.ShouldBe([42]);
    }

    [Fact]
    public void Stream_TerminatorThatStopsImmediatelyStillIncludesTriggeringState()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var terminator = new RecordingTerminator(_ => true);
        var algorithm = CreateStateTerminatedAlgorithm(terminator);

        var states = algorithm.Stream(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).ToList();

        states.Select(MetaAlgorithmTestHelpers.StateCandidate).ShouldBe([1]);
        terminator.CheckedCandidates.ShouldBe([1]);
    }

    [Fact]
    public void WithMaxIterations_YieldsTriggeringStateBeforeStopping()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var algorithm = new AdditiveStepAlgorithm(1).WithMaxIterations(1);

        var states = algorithm.Stream(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).ToList();

        states.Select(MetaAlgorithmTestHelpers.StateCandidate).ShouldBe([1]);
    }

    [Fact]
    public void WithMaxIterations_StopsOnFirstCheck_WhenMaximumIterationsIsNotPositive()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var algorithm = new AdditiveStepAlgorithm(1).WithMaxIterations(0);

        var states = algorithm.Stream(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).ToList();

        states.Select(MetaAlgorithmTestHelpers.StateCandidate).ShouldBe([1]);
    }

    [Fact]
    public void Stream_WithCanceledRunToken_InterruptsBeforeProducingState()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Should.Throw<OperationCanceledException>(() => new AdditiveStepAlgorithm(1).Stream(problem, RandomNumberGenerator.Create(42), ct: cts.Token).ToList());
    }

    [Fact]
    public void Stream_WithCanceledTerminatorToken_YieldsProducedStateThenStops()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var algorithm = CreateStateTerminatedAlgorithm(new CancellationTokenTerminator<int>(cts.Token));

        var states = algorithm.Stream(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).ToList();

        states.Select(MetaAlgorithmTestHelpers.StateCandidate).ShouldBe([1]);
    }

    [Fact]
    public void Stream_WithTerminatorTokenCanceledImmediatelyBeforeRun_YieldsProducedStateThenStops()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        using var cts = new CancellationTokenSource();
        var algorithm = CreateStateTerminatedAlgorithm(new CancellationTokenTerminator<int>(cts.Token));

        cts.Cancel();
        var states = algorithm.Stream(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).ToList();

        states.Select(MetaAlgorithmTestHelpers.StateCandidate).ShouldBe([1]);
    }

    [Fact]
    public void AfterElapsedTimeTerminator_UsesElapsedTimeFromExecutionInstanceCreation()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var timeProvider = new ManualTimeProvider();
        var terminator = new AfterElapsedTimeTerminator<int>(TimeSpan.FromSeconds(5)) { TimeProvider = timeProvider };
        var instance = new ExecutionInstanceRegistry().Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>(terminator);

        instance.IsTerminalState(CreateState(1), problem.SearchSpace, problem).ShouldBeFalse();

        timeProvider.Advance(TimeSpan.FromSeconds(5));

        instance.IsTerminalState(CreateState(2), problem.SearchSpace, problem).ShouldBeTrue();
    }

    [Fact]
    public void AfterElapsedTimeTerminator_StopsOnFirstCheck_WhenMaximumElapsedTimeIsNotPositive()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var instance = new ExecutionInstanceRegistry().Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>(new AfterElapsedTimeTerminator<int>(TimeSpan.Zero));

        instance.IsTerminalState(CreateState(1), problem.SearchSpace, problem).ShouldBeTrue();
    }

    [Fact]
    public void CreateExecutionInstance_ResolvesTerminatorBeforeWrappedAlgorithm()
    {
        var events = new List<string>();
        var algorithm = new RecordingAlgorithm(events).WithTerminator(new RecordingResolveTerminator(events));

        _ = algorithm.CreateExecutionInstance();

        events.ShouldBe(["terminator", "algorithm"]);
    }

    private static StateTerminatedAlgorithm<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>> CreateStateTerminatedAlgorithm(ITerminator<int> terminator)
    {
        return new AdditiveStepAlgorithm(1).WithTerminator(terminator);
    }

    private static PopulationState<int> CreateState(int candidate)
    {
        return Population.From([EvaluatedCandidate.From(candidate, candidate)]).ToPopulationState();
    }

    private sealed record RecordingTerminator(Func<int, bool> ShouldStop)
        : StatelessTerminator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>
    {
        public List<int> CheckedCandidates { get; } = [];

        public override bool IsTerminalState(PopulationState<int> state, DummySearchSpace<int> searchSpace, IProblem<int, DummySearchSpace<int>> problem)
        {
            var candidate = MetaAlgorithmTestHelpers.StateCandidate(state);
            CheckedCandidates.Add(candidate);
            return ShouldStop(candidate);
        }
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private long timestamp;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp()
        {
            return timestamp;
        }

        public void Advance(TimeSpan elapsed)
        {
            timestamp += elapsed.Ticks;
        }
    }

    private sealed record RecordingAlgorithm(List<string> Events)
        : Algorithm<RecordingAlgorithm, int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>
    {
        public override AlgorithmInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
        {
            Events.Add("algorithm");
            return new Instance();
        }

        private sealed class Instance
            : AlgorithmInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>
        {
            public override async IAsyncEnumerable<PopulationState<int>> RunStreamingAsync(IProblem<int, DummySearchSpace<int>> problem, IRandomNumberGenerator random, PopulationState<int>? initialState = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
            {
                await Task.CompletedTask;
                yield break;
            }
        }
    }

    private sealed record RecordingResolveTerminator(List<string> Events)
        : ITerminator<int>
    {
        public ITerminatorInstance<int, TRunSearchSpace, TRunProblem, TRunSearchState> CreateExecutionInstance<TRunSearchSpace, TRunProblem, TRunSearchState>(ExecutionInstanceRegistry instanceRegistry)
            where TRunSearchSpace : class, ISearchSpace<int>
            where TRunProblem : class, IProblem<int, TRunSearchSpace>
            where TRunSearchState : class, ISearchState
        {
            Events.Add("terminator");
            return (ITerminatorInstance<int, TRunSearchSpace, TRunProblem, TRunSearchState>)(object)new Instance();
        }

        private sealed class Instance : ITerminatorInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>
        {
            public bool IsTerminalState(PopulationState<int> state, DummySearchSpace<int> searchSpace, IProblem<int, DummySearchSpace<int>> problem)
            {
                return false;
            }
        }
    }
}
