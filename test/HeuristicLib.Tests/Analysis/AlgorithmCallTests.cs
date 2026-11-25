using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Analysis;

// Covers the start and end of each algorithm call, and the turns of a cycle observed through them.
public sealed class AlgorithmCallTests
{
    private static readonly IProblem<int, DummySearchSpace<int>> Problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();

    [Fact]
    public async Task ACall_ReportsItsStartAndCompletion()
    {
        IAlgorithm<int, PopulationState<int>> algorithm = new ScriptedAlgorithm(States: 2);
        var events = new List<AlgorithmCallObservation<int, PopulationState<int>>>();
        var executor = Resolve(ResolutionScope.Create(), ObservingCalls(algorithm, events));
        await using var states = executor.RunStreamingAsync(RandomNumberGenerator.Create(1), ct: TestContext.Current.CancellationToken)
            .GetAsyncEnumerator(TestContext.Current.CancellationToken);

        events.ShouldBeEmpty();
        var yielded = 0;
        while (await states.MoveNextAsync())
            yielded++;
        yielded.ShouldBe(2);

        events.Select(call => (call.Event, call.Iterations)).ShouldBe([(AlgorithmCallEvent.Started, 0L), (AlgorithmCallEvent.Completed, 2L)]);
        events[0].State.ShouldBeNull();
        events[1].State.ShouldNotBeNull().Population.EvaluatedCandidates.Single().Candidate.ShouldBe(2);
        events.ShouldAllBe(call => ReferenceEquals(call.Algorithm, algorithm));
    }

    [Fact]
    public async Task ACallThatFails_ReportsFailed()
    {
        IAlgorithm<int, PopulationState<int>> algorithm = new ScriptedAlgorithm(States: 1, Failure: new InvalidOperationException("Step failed."));
        var events = new List<AlgorithmCallObservation<int, PopulationState<int>>>();

        await Should.ThrowAsync<InvalidOperationException>(() => Run(Resolve(ResolutionScope.Create(), ObservingCalls(algorithm, events))));

        events.Select(call => (call.Event, call.Iterations)).ShouldBe([(AlgorithmCallEvent.Started, 0L), (AlgorithmCallEvent.Failed, 1L)]);
    }

    [Fact]
    public async Task ACanceledCall_ReportsStopped()
    {
        IAlgorithm<int, PopulationState<int>> algorithm = new ScriptedAlgorithm(States: 1, Failure: new OperationCanceledException());
        var events = new List<AlgorithmCallObservation<int, PopulationState<int>>>();

        await Should.ThrowAsync<OperationCanceledException>(() => Run(Resolve(ResolutionScope.Create(), ObservingCalls(algorithm, events))));

        events.Select(call => call.Event).ShouldBe([AlgorithmCallEvent.Started, AlgorithmCallEvent.Stopped]);
    }

    [Fact]
    public async Task ACallWhoseCallerStopsEarly_ReportsStopped()
    {
        IAlgorithm<int, PopulationState<int>> algorithm = new ScriptedAlgorithm(States: 3);
        var events = new List<AlgorithmCallObservation<int, PopulationState<int>>>();
        var executor = Resolve(ResolutionScope.Create(), ObservingCalls(algorithm, events));

        await using (var states = executor.RunStreamingAsync(RandomNumberGenerator.Create(1), ct: TestContext.Current.CancellationToken)
                         .GetAsyncEnumerator(TestContext.Current.CancellationToken))
            (await states.MoveNextAsync()).ShouldBeTrue();

        events.Select(call => (call.Event, call.Iterations)).ShouldBe([(AlgorithmCallEvent.Started, 0L), (AlgorithmCallEvent.Stopped, 1L)]);
    }

    // A paused run keeps its call to resume it, so the call ends only when the run does.
    [Fact]
    public async Task APausedRun_ReportsNoEndUntilItResumesAndCompletes()
    {
        IAlgorithm<int, PopulationState<int>> algorithm = new ScriptedAlgorithm(States: 2);
        var calls = algorithm.Calls.Trace(call => call.Event);
        var run = algorithm.CreateRun(Problem, RandomNumberGenerator.Create(1), [calls]);
        await using (var states = run.Stream(cancellationToken: TestContext.Current.CancellationToken).GetAsyncEnumerator(TestContext.Current.CancellationToken))
            (await states.MoveNextAsync()).ShouldBeTrue();

        run.LifecycleState.ShouldBe(RunLifecycleState.Paused);
        run.Result(calls).Snapshot().Values.ShouldBe([AlgorithmCallEvent.Started]);
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);
        run.Result(calls).Snapshot().Values.ShouldBe([AlgorithmCallEvent.Started, AlgorithmCallEvent.Completed]);
    }

    // The call counter counts calls when they start, so a turn's number is known while it runs, and a stopped call
    // counts too; the iteration counter counts yielded states across calls.
    [Fact]
    public async Task CallAndIterationCounters_CountStartedCallsAndYieldedStates()
    {
        IAlgorithm<int, PopulationState<int>> algorithm = new ScriptedAlgorithm(States: 3);
        var calls = new Counter();
        var iterations = new Counter();
        var root = ResolutionScope.Create();
        var executor = Resolve(root, algorithm.Substitute(algorithm.Calls.Counting(calls), algorithm.Iterations.Counting(iterations)));

        await Run(executor);
        await using (var states = executor.RunStreamingAsync(RandomNumberGenerator.Create(1), ct: TestContext.Current.CancellationToken)
                         .GetAsyncEnumerator(TestContext.Current.CancellationToken))
        {
            (await states.MoveNextAsync()).ShouldBeTrue();
            root.Results.Result(calls).CurrentCount.ShouldBe(2);
        }

        root.Results.Result(calls).CurrentCount.ShouldBe(2);
        root.Results.Result(iterations).CurrentCount.ShouldBe(4);
    }

    // Each turn of a cycle is a call of an inner algorithm, so observing the inner algorithms' calls records every
    // turn's start and end, also a turn that yields nothing. Attaching the trace to the cycle and placing it on each
    // inner algorithm record the same calls, and both work with the fresh executors each turn builds.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CycleTurns_AreRecordedThroughBothPaths_IncludingATurnYieldingNothing(bool attached)
    {
        var explore = new ScriptedAlgorithm(States: 2);
        var idle = new ScriptedAlgorithm(States: 0);
        var turns = Analyzer.Trace([explore.Calls, idle.Calls],
            call => (Turn: ReferenceEquals(call.Algorithm, explore) ? "explore" : "idle", call.Event, call.Iterations));
        IAlgorithm<int, PopulationState<int>> cycle = attached
            ? (CycleAlgorithm.Create<int, PopulationState<int>>(explore, idle) with { MaximumCycles = 2 }).Substitute(turns)
            : CycleAlgorithm.Create<int, PopulationState<int>>(explore.Observed(turns), idle.Observed(turns)) with { MaximumCycles = 2 };
        var run = cycle.CreateRun(Problem, RandomNumberGenerator.Create(1));

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        run.Result(turns).Snapshot().Values.ShouldBe([
            ("explore", AlgorithmCallEvent.Started, 0L), ("explore", AlgorithmCallEvent.Completed, 2L),
            ("idle", AlgorithmCallEvent.Started, 0L), ("idle", AlgorithmCallEvent.Completed, 0L),
            ("explore", AlgorithmCallEvent.Started, 0L), ("explore", AlgorithmCallEvent.Completed, 2L),
            ("idle", AlgorithmCallEvent.Started, 0L), ("idle", AlgorithmCallEvent.Completed, 0L)
        ]);
    }

    // Turn numbers come from a counter of the inner algorithm's calls: the states a turn yields and the turn's end read
    // the number of the turn. The start of a turn reads the counter before its own call is counted.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TurnNumbers_ComeFromACounterOfTheInnerCalls(bool attached)
    {
        var turn = new ScriptedAlgorithm(States: 1);
        var turnCounter = new Counter();
        var states = turn.Iterations.Trace(observation => observation.Iteration, [turnCounter.Clock]);
        var calls = turn.Calls.Trace(call => call.Event, [turnCounter.Clock]);
        IAlgorithm<int, PopulationState<int>> cycle;
        if (attached)
            cycle = (CycleAlgorithm.Create(turn, turn) with { MaximumCycles = 2 }).Substitute(states, calls, turn.Calls.Counting(turnCounter));
        else
        {
            var placed = new CountedAlgorithm<int, PopulationState<int>, AlgorithmCallObservation<int, PopulationState<int>>>(turn, turnCounter).Observed(calls).Observed(states);
            cycle = CycleAlgorithm.Create<int, PopulationState<int>>(placed, placed) with { MaximumCycles = 2 };
        }
        var run = cycle.CreateRun(Problem, RandomNumberGenerator.Create(1));

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        run.Result(states).By(turnCounter.Clock).Select(point => point.Time).ShouldBe([1L, 2L, 3L, 4L]);
        run.Result(calls).Snapshot().Where(entry => entry.Value == AlgorithmCallEvent.Completed)
            .Select(entry => entry.At(turnCounter.Clock)).ShouldBe([1L, 2L, 3L, 4L]);
        run.Result(calls).Snapshot().Where(entry => entry.Value == AlgorithmCallEvent.Started)
            .Select(entry => entry.At(turnCounter.Clock)).ShouldBe([0L, 1L, 2L, 3L]);
    }

    [Fact]
    public async Task ACallWhoseStreamFailsToStart_ReportsFailed()
    {
        var algorithm = new FailingToStartAlgorithm();
        var events = new List<AlgorithmCallObservation<int, PopulationState<int>>>();

        await Should.ThrowAsync<InvalidOperationException>(() => Run(Resolve(ResolutionScope.Create(), ObservingCalls(algorithm, events))));

        events.Select(call => call.Event).ShouldBe([AlgorithmCallEvent.Started, AlgorithmCallEvent.Failed]);
    }

    [Fact]
    public async Task ACallStoppedEarly_ReportsStoppedBeforeItsDisposalFails()
    {
        var algorithm = new FailingDisposalAlgorithm();
        var events = new List<AlgorithmCallObservation<int, PopulationState<int>>>();
        var executor = Resolve(ResolutionScope.Create(), ObservingCalls(algorithm, events));
        var states = executor.RunStreamingAsync(RandomNumberGenerator.Create(1), ct: TestContext.Current.CancellationToken)
            .GetAsyncEnumerator(TestContext.Current.CancellationToken);
        (await states.MoveNextAsync()).ShouldBeTrue();

        await Should.ThrowAsync<InvalidOperationException>(async () => await states.DisposeAsync());

        events.Select(call => call.Event).ShouldBe([AlgorithmCallEvent.Started, AlgorithmCallEvent.Stopped]);
    }

    private static IAlgorithm<int, PopulationState<int>> ObservingCalls(IAlgorithm<int, PopulationState<int>> algorithm, List<AlgorithmCallObservation<int, PopulationState<int>>> events) =>
        algorithm.Substitute(algorithm.Calls.Observing(events.Add));

    private static IAlgorithmExecutor<PopulationState<int>> Resolve(
        ResolutionScope scope, IAlgorithm<int, PopulationState<int>> algorithm) =>
        scope.Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>(algorithm, Problem);

    private static async Task<int> Run(IAlgorithmExecutor<PopulationState<int>> executor)
    {
        var states = 0;
        await foreach (var _ in executor.RunStreamingAsync(RandomNumberGenerator.Create(1), ct: TestContext.Current.CancellationToken))
            states++;
        return states;
    }

    // Yields the given number of states, counting up from the state it starts from, then fails if given a failure.
    private sealed record ScriptedAlgorithm(int States, Exception? Failure = null) : Algorithm<ScriptedAlgorithm, int, PopulationState<int>>
    {
        protected override IAlgorithmExecutor<PopulationState<int>> CreateExecutor<TSearchSpace, TProblem>(
            ResolutionScope<int, TSearchSpace, TProblem, PopulationState<int>> scope) =>
            new Executor(States, Failure);

        private sealed class Executor(int states, Exception? failure)
            : AlgorithmExecutor<PopulationState<int>>
        {
            public override async IAsyncEnumerable<PopulationState<int>> RunStreamingAsync(IRandomNumberGenerator random, PopulationState<int>? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
            {
                var current = initialState?.Population.EvaluatedCandidates.Single().Candidate ?? 0;
                for (var state = 0; state < states; state++)
                {
                    current++;
                    yield return Population.From([current.ToEvaluated(new ObjectiveVector(current))]).ToPopulationState();
                }
                if (failure is not null)
                    throw failure;
                await Task.CompletedTask;
            }
        }
    }

    // Fails when its stream is requested, before any state is asked for.
    private sealed record FailingToStartAlgorithm : Algorithm<FailingToStartAlgorithm, int, PopulationState<int>>
    {
        protected override IAlgorithmExecutor<PopulationState<int>> CreateExecutor<TSearchSpace, TProblem>(
            ResolutionScope<int, TSearchSpace, TProblem, PopulationState<int>> scope) =>
            new Executor();

        private sealed class Executor : AlgorithmExecutor<PopulationState<int>>
        {

            public override IAsyncEnumerable<PopulationState<int>> RunStreamingAsync(IRandomNumberGenerator random, PopulationState<int>? initialState = null, CancellationToken ct = default) =>
                throw new InvalidOperationException("The stream cannot start.");
        }
    }

    // Yields states until its caller stops, and fails when its stream is disposed.
    private sealed record FailingDisposalAlgorithm : Algorithm<FailingDisposalAlgorithm, int, PopulationState<int>>
    {
        protected override IAlgorithmExecutor<PopulationState<int>> CreateExecutor<TSearchSpace, TProblem>(
            ResolutionScope<int, TSearchSpace, TProblem, PopulationState<int>> scope) =>
            new Executor();

        private sealed class Executor : AlgorithmExecutor<PopulationState<int>>
        {

            public override IAsyncEnumerable<PopulationState<int>> RunStreamingAsync(IRandomNumberGenerator random, PopulationState<int>? initialState = null, CancellationToken ct = default) =>
                new States();
        }

        private sealed class States : IAsyncEnumerable<PopulationState<int>>, IAsyncEnumerator<PopulationState<int>>
        {
            public PopulationState<int> Current { get; } = Population.From([1.ToEvaluated(new ObjectiveVector(1))]).ToPopulationState();

            public IAsyncEnumerator<PopulationState<int>> GetAsyncEnumerator(CancellationToken cancellationToken = default) => this;

            public ValueTask<bool> MoveNextAsync() => ValueTask.FromResult(true);

            public ValueTask DisposeAsync() => ValueTask.FromException(new InvalidOperationException("Disposal failed."));
        }
    }
}
