using System.Runtime.CompilerServices;
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
    public void Stream_GivesTheTerminatorAStreamPerStateApartFromTheIterationStreams()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var iterationDraws = new List<double>();
        var terminator = new DrawingTerminator(StopAfter: 3);

        var states = new DrawingIterativeAlgorithm(iterationDraws).TerminatedBy(terminator)
            .Stream(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).ToList();
        var repeated = new DrawingTerminator(StopAfter: 3);
        _ = new DrawingIterativeAlgorithm([]).TerminatedBy(repeated)
            .Stream(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).ToList();
        var undrawnIterationDraws = new List<double>();
        _ = new DrawingIterativeAlgorithm(undrawnIterationDraws).TerminatedAfterIterations(3)
            .Stream(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).ToList();

        states.Select(MetaAlgorithmTestHelpers.StateCandidate).ShouldBe([1, 2, 3]);
        terminator.Draws.Distinct().Count().ShouldBe(3);
        terminator.Draws.ShouldNotContain(draw => iterationDraws.Contains(draw));
        repeated.Draws.ShouldBe(terminator.Draws);
        iterationDraws.ShouldBe(undrawnIterationDraws);
    }

    [Fact]
    public void WithMaxIterations_YieldsTriggeringStateBeforeStopping()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var algorithm = new AdditiveStepAlgorithm(1).TerminatedAfterIterations(1);

        var states = algorithm.Stream(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).ToList();

        states.Select(MetaAlgorithmTestHelpers.StateCandidate).ShouldBe([1]);
    }

    [Fact]
    public void WithMaxIterations_StopsOnFirstCheck_WhenMaximumIterationsIsNotPositive()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var algorithm = new AdditiveStepAlgorithm(1).TerminatedAfterIterations(0);

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
    public void AfterElapsedTimeTerminator_UsesElapsedTimeFromExecutionCreation()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var timeProvider = new ManualTimeProvider();
        var terminator = new AfterElapsedTimeTerminator<int>(TimeSpan.FromSeconds(5)) { TimeProvider = timeProvider };
        var execution = ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>(terminator);

        execution.IsTerminalState(CreateState(1), RandomNumberGenerator.Create(0), problem.SearchSpace, problem).ShouldBeFalse();

        timeProvider.Advance(TimeSpan.FromSeconds(5));

        execution.IsTerminalState(CreateState(2), RandomNumberGenerator.Create(0), problem.SearchSpace, problem).ShouldBeTrue();
    }

    [Fact]
    public void AfterElapsedTimeTerminator_StopsOnFirstCheck_WhenMaximumElapsedTimeIsNotPositive()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var execution = ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>(new AfterElapsedTimeTerminator<int>(TimeSpan.Zero));

        execution.IsTerminalState(CreateState(1), RandomNumberGenerator.Create(0), problem.SearchSpace, problem).ShouldBeTrue();
    }

    [Fact]
    public void Binding_ResolvesTerminatorBeforeWrappedAlgorithm()
    {
        var events = new List<string>();
        var algorithm = new RecordingAlgorithm(events).TerminatedBy(new RecordingResolveTerminator(events));

        _ = ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>(algorithm);

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

    private sealed record RecordingTerminator(Func<int, bool> ShouldStop)
        : StatelessTerminator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>
    {
        public List<int> CheckedCandidates { get; } = [];

        public override bool IsTerminalState(PopulationState<int> state, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace, IProblem<int, DummySearchSpace<int>> problem)
        {
            var candidate = MetaAlgorithmTestHelpers.StateCandidate(state);
            CheckedCandidates.Add(candidate);
            return ShouldStop(candidate);
        }
    }

    private sealed record DrawingTerminator(int StopAfter)
        : StatelessTerminator<int, PopulationState<int>>
    {
        public List<double> Draws { get; } = [];

        public override bool IsTerminalState(PopulationState<int> state, IRandomNumberGenerator random)
        {
            Draws.Add(random.NextDouble());
            return Draws.Count >= StopAfter;
        }
    }

    private sealed record DrawingIterativeAlgorithm(List<double> Draws)
        : IterativeAlgorithm<DrawingIterativeAlgorithm, int, PopulationState<int>>
    {
        protected override ExecutorFactory<IterativeAlgorithmExecutor<int, TRunSearchSpace, TRunProblem, PopulationState<int>>> CreateIterationFactory<TRunSearchSpace, TRunProblem>() =>
            _ => new Executor<TRunSearchSpace, TRunProblem>(Draws);

        private sealed class Executor<TSearchSpace, TProblem>(List<double> draws)
            : IterativeAlgorithmExecutor<int, TSearchSpace, TProblem, PopulationState<int>>(null)
            where TSearchSpace : class, ISearchSpace<int>
            where TProblem : class, IProblem<int, TSearchSpace>
        {
            protected override PopulationState<int> ExecuteStep(PopulationState<int>? previousState, TProblem problem, IRandomNumberGenerator random)
            {
                draws.Add(random.NextDouble());
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

    private sealed record RecordingAlgorithm(List<string> Events)
        : Algorithm<RecordingAlgorithm, int, PopulationState<int>>
    {
        public override ExecutorFactory<IAlgorithmExecutor<int, TRunSearchSpace, TRunProblem, PopulationState<int>>> CreateExecutorFactory<TRunSearchSpace, TRunProblem>()
        {
            Events.Add("algorithm");
            var executor = new Executor<TRunSearchSpace, TRunProblem>();
            return _ => executor;
        }

        private sealed class Executor<TSearchSpace, TProblem>
            : AlgorithmExecutor<int, TSearchSpace, TProblem, PopulationState<int>>
            where TSearchSpace : class, ISearchSpace<int>
            where TProblem : class, IProblem<int, TSearchSpace>
        {
            public override async IAsyncEnumerable<PopulationState<int>> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random, PopulationState<int>? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
            {
                await Task.CompletedTask;
                yield break;
            }
        }
    }

    private sealed record RecordingResolveTerminator(List<string> Events)
        : Operator<Termination, int>
    {
        protected override OperatorExecutorFactory CreateOperatorFactory()
        {
            Events.Add("terminator");
            return new Factory();
        }

        private sealed class Factory : OperatorExecutorFactory<Termination, int>
        {
            protected override IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(ResolutionScope<int, TSearchSpace, TProblem, TSearchState> scope) =>
                new Executor<TSearchSpace, TProblem, TSearchState>();
        }

        private sealed class Executor<TSearchSpace, TProblem, TSearchState> : TerminationExecutor<int, TSearchSpace, TProblem, TSearchState>
            where TSearchSpace : class, ISearchSpace<int>
            where TProblem : class, IProblem<int, TSearchSpace>
            where TSearchState : class, ISearchState
        {
            public override bool IsTerminalState(TSearchState state, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) => false;
        }
    }
}
