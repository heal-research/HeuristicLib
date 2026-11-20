using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Algorithms.MetaAlgorithms;

public class StateTerminatedAlgorithmTests
{
    [Fact]
    public void RunStreaming_DoesNotCheckSuppliedInitialState()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var checkedCandidates = new List<int>();
        var terminator = new RecordingTerminator(_ => false, checkedCandidates.Add);
        var algorithm = CreateStateTerminatedAlgorithm(terminator);
        var initialState = CreateState(41);

        var states = algorithm.RunStreaming(problem, RandomNumberGenerator.Create(42), initialState, TestContext.Current.CancellationToken).ToList();

        states.Select(MetaAlgorithmTestHelpers.StateCandidate).ShouldBe([42]);
        checkedCandidates.ShouldBe([42]);
    }

    [Fact]
    public void RunStreaming_YieldsFirstProducedStateBeforeCheckingTerminator()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var checkedCandidates = new List<int>();
        var terminator = new RecordingTerminator(_ => true, checkedCandidates.Add);
        var algorithm = CreateStateTerminatedAlgorithm(terminator);
        var initialState = CreateState(41);

        var states = algorithm.RunStreaming(problem, RandomNumberGenerator.Create(42), initialState, TestContext.Current.CancellationToken).ToList();

        states.Select(MetaAlgorithmTestHelpers.StateCandidate).ShouldBe([42]);
        checkedCandidates.ShouldBe([42]);
    }

    [Fact]
    public void RunStreaming_TerminatorThatStopsImmediatelyStillIncludesTriggeringState()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var checkedCandidates = new List<int>();
        var terminator = new RecordingTerminator(_ => true, checkedCandidates.Add);
        var algorithm = CreateStateTerminatedAlgorithm(terminator);

        var states = algorithm.RunStreaming(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).ToList();

        states.Select(MetaAlgorithmTestHelpers.StateCandidate).ShouldBe([1]);
        checkedCandidates.ShouldBe([1]);
    }

    [Fact]
    public void RunStreaming_GivesTheTerminatorAStreamPerStateApartFromTheIterationStreams()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var iterationDraws = new List<double>();
        var terminatorDraws = new List<double>();
        var terminator = new DrawingTerminator(terminatorDraws.Add, () => terminatorDraws.Count >= 3);

        var states = new DrawingIterativeAlgorithm(iterationDraws.Add).TerminatedBy(terminator)
            .RunStreaming(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).ToList();
        var repeatedDraws = new List<double>();
        var repeated = new DrawingTerminator(repeatedDraws.Add, () => repeatedDraws.Count >= 3);
        _ = new DrawingIterativeAlgorithm(_ => { }).TerminatedBy(repeated)
            .RunStreaming(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).ToList();
        var undrawnIterationDraws = new List<double>();
        _ = new DrawingIterativeAlgorithm(undrawnIterationDraws.Add).TerminatedAfterIterations(3)
            .RunStreaming(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).ToList();

        states.Select(MetaAlgorithmTestHelpers.StateCandidate).ShouldBe([1, 2, 3]);
        terminatorDraws.Distinct().Count().ShouldBe(3);
        terminatorDraws.ShouldNotContain(draw => iterationDraws.Contains(draw));
        repeatedDraws.ShouldBe(terminatorDraws);
        iterationDraws.ShouldBe(undrawnIterationDraws);
    }

    [Fact]
    public void WithMaxIterations_YieldsTriggeringStateBeforeStopping()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var algorithm = new AdditiveStepAlgorithm(1).TerminatedAfterIterations(1);

        var states = algorithm.RunStreaming(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).ToList();

        states.Select(MetaAlgorithmTestHelpers.StateCandidate).ShouldBe([1]);
    }

    [Fact]
    public void WithMaxIterations_StopsOnFirstCheck_WhenMaximumIterationsIsNotPositive()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var algorithm = new AdditiveStepAlgorithm(1).TerminatedAfterIterations(0);

        var states = algorithm.RunStreaming(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).ToList();

        states.Select(MetaAlgorithmTestHelpers.StateCandidate).ShouldBe([1]);
    }

    [Fact]
    public void RunStreaming_WithCanceledRunToken_InterruptsBeforeProducingState()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Should.Throw<OperationCanceledException>(() => new AdditiveStepAlgorithm(1).RunStreaming(problem, RandomNumberGenerator.Create(42), ct: cts.Token).ToList());
    }

    [Fact]
    public void RunStreaming_WithCanceledTerminatorToken_YieldsProducedStateThenStops()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var algorithm = CreateStateTerminatedAlgorithm(new CancellationTokenTerminator<int>(cts.Token));

        var states = algorithm.RunStreaming(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).ToList();

        states.Select(MetaAlgorithmTestHelpers.StateCandidate).ShouldBe([1]);
    }

    [Fact]
    public void RunStreaming_WithTerminatorTokenCanceledImmediatelyBeforeRun_YieldsProducedStateThenStops()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        using var cts = new CancellationTokenSource();
        var algorithm = CreateStateTerminatedAlgorithm(new CancellationTokenTerminator<int>(cts.Token));

        cts.Cancel();
        var states = algorithm.RunStreaming(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).ToList();

        states.Select(MetaAlgorithmTestHelpers.StateCandidate).ShouldBe([1]);
    }

    [Fact]
    public void AfterElapsedTimeTerminator_UsesElapsedTimeFromExecutionCreation()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var timeProvider = new ManualTimeProvider();
        var terminator = new AfterElapsedTimeTerminator<int>(TimeSpan.FromSeconds(5));
        var executor = ResolutionScope.Create(timeProvider).Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>(terminator, problem);

        executor.IsTerminalState(CreateState(1), RandomNumberGenerator.Create(0)).ShouldBeFalse();

        timeProvider.Advance(TimeSpan.FromSeconds(5));

        executor.IsTerminalState(CreateState(2), RandomNumberGenerator.Create(0)).ShouldBeTrue();
    }

    [Fact]
    public void AfterElapsedTimeTerminator_StopsOnFirstCheck_WhenMaximumElapsedTimeIsNotPositive()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var executor = ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>(new AfterElapsedTimeTerminator<int>(TimeSpan.Zero), problem);

        executor.IsTerminalState(CreateState(1), RandomNumberGenerator.Create(0)).ShouldBeTrue();
    }

    [Fact]
    public void Binding_ResolvesTerminatorBeforeWrappedAlgorithm()
    {
        var events = new List<string>();
        var algorithm = new RecordingAlgorithm(events.Add).TerminatedBy(new RecordingResolveTerminator(events.Add));

        _ = ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>(algorithm, MetaAlgorithmTestHelpers.CreateIntegerProblem());

        events.ShouldBe(["terminator", "algorithm"]);
    }

    private static StateTerminatedAlgorithm<int, PopulationState<int>> CreateStateTerminatedAlgorithm(IOperator<Termination, int> terminator)
    {
        return new AdditiveStepAlgorithm(1).TerminatedBy(terminator);
    }

    private static PopulationState<int> CreateState(int candidate)
    {
        return Population.From([EvaluatedCandidate.From(candidate, candidate)]).ToPopulationState();
    }

    private sealed record RecordingTerminator(Func<int, bool> ShouldStop, Action<int> RecordCandidate)
        : StatelessTerminator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>
    {
        public override bool IsTerminalState(PopulationState<int> state, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace, IProblem<int, DummySearchSpace<int>> problem)
        {
            var candidate = MetaAlgorithmTestHelpers.StateCandidate(state);
            RecordCandidate(candidate);
            return ShouldStop(candidate);
        }
    }

    private sealed record DrawingTerminator(Action<double> RecordDraw, Func<bool> ShouldStop)
        : StatelessTerminator<int, PopulationState<int>>
    {
        public override bool IsTerminalState(PopulationState<int> state, IRandomNumberGenerator random)
        {
            RecordDraw(random.NextDouble());
            return ShouldStop();
        }
    }

    private sealed record DrawingIterativeAlgorithm(Action<double> RecordDraw)
        : IterativeAlgorithm<DrawingIterativeAlgorithm, int, PopulationState<int>>
    {
        protected override IterativeAlgorithmExecutor<PopulationState<int>> CreateExecutor<TSearchSpace, TProblem>(
            ResolutionScope<int, TSearchSpace, TProblem, PopulationState<int>> scope) =>
            new Executor(RecordDraw);

        private sealed class Executor(Action<double> recordDraw)
            : IterativeAlgorithmExecutor<PopulationState<int>>(null)
        {
            protected override PopulationState<int> ExecuteStep(PopulationState<int>? previousState, IRandomNumberGenerator random)
            {
                recordDraw(random.NextDouble());
                return CreateState(previousState is null ? 1 : MetaAlgorithmTestHelpers.StateCandidate(previousState) + 1);
            }
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

    private sealed record RecordingAlgorithm(Action<string> RecordEvent)
        : Algorithm<RecordingAlgorithm, int, PopulationState<int>>
    {
        protected override IAlgorithmExecutor<PopulationState<int>> CreateExecutor<TSearchSpace, TProblem>(
            ResolutionScope<int, TSearchSpace, TProblem, PopulationState<int>> scope)
        {
            RecordEvent("algorithm");
            return new Executor();
        }

        private sealed class Executor
            : AlgorithmExecutor<PopulationState<int>>
        {

            public override async IAsyncEnumerable<PopulationState<int>> RunStreamingAsync(IRandomNumberGenerator random, PopulationState<int>? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
            {
                await Task.CompletedTask;
                yield break;
            }
        }
    }

    private sealed record RecordingResolveTerminator(Action<string> RecordEvent)
        : Operator<Termination, int>
    {
        protected override IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(ResolutionScope<int, TSearchSpace, TProblem, TSearchState> scope)
        {
            RecordEvent("terminator");
            return new Executor<TSearchState>();
        }

        private sealed class Executor<TSearchState> : TerminationExecutor<TSearchState>
            where TSearchState : class, ISearchState
        {
            public override bool IsTerminalState(TSearchState state, IRandomNumberGenerator random) => false;
        }
    }
}
