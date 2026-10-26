using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Algorithms;

public class IterativeAlgorithmExecutionTests
{
    [Fact]
    public async Task HasCompleted_IsCheckedBeforeExecutingAStep()
    {
        var executor = new ProbeExecutor { CompleteAtCount = 0 };

        var states = await Collect(executor, ct: TestContext.Current.CancellationToken);

        states.ShouldBeEmpty();
        executor.StepCalls.ShouldBe(0);
        executor.Events.ShouldBe(["complete:0"]);
    }

    [Fact]
    public async Task Cancellation_IsCheckedBeforeExecutingAStep()
    {
        var executor = new ProbeExecutor();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(async () => await Collect(executor, ct: cts.Token));

        executor.StepCalls.ShouldBe(0);
    }

    [Fact]
    public async Task RandomForks_UseTheYieldedStateCount()
    {
        var executor = new ProbeExecutor { CompleteAtCount = 2 };
        var random = new RecordingRandom();

        _ = await Collect(executor, random, ct: TestContext.Current.CancellationToken);

        random.ForkKeys.ShouldBe([0UL, 1UL]);
    }

    [Fact]
    public async Task TryExecuteStepReturningFalse_DoesNotYieldOrIntercept()
    {
        var interceptor = new RecordingInterceptor();
        var executor = new ProbeExecutor(interceptor) { StopBeforeFirstStep = true };

        var states = await Collect(executor, ct: TestContext.Current.CancellationToken);

        states.ShouldBeEmpty();
        interceptor.Invocations.ShouldBe(0);
    }

    [Fact]
    public async Task Interception_PrecedesTerminalStateEvaluation()
    {
        var interceptor = new RecordingInterceptor();
        var executor = new ProbeExecutor(interceptor) { TerminalAtValue = 2 };

        var states = await Collect(executor, ct: TestContext.Current.CancellationToken);

        states.Select(state => state.Value).ShouldBe([2]);
        executor.Events.ShouldBe(["complete:0", "step", "terminal:2"]);
    }

    [Fact]
    public async Task Interceptor_ReceivesTheIterationRandom()
    {
        var interceptor = new RecordingInterceptor();
        var random = new RecordingRandom();
        var executor = new ProbeExecutor(interceptor) { TerminalAtValue = 2 };

        _ = await Collect(executor, random, ct: TestContext.Current.CancellationToken);

        interceptor.Random.ShouldBeSameAs(random);
    }

    [Fact]
    public async Task TerminalStateEvaluation_ReceivesTheInterceptorsIterationRandom()
    {
        var interceptor = new RecordingInterceptor();
        var random = RandomNumberGenerator.Create(1);
        var executor = new ProbeExecutor(interceptor) { TerminalAtValue = 2 };

        _ = await Collect(executor, random, ct: TestContext.Current.CancellationToken);

        executor.TerminalStateRandom.ShouldNotBeNull().ShouldBeSameAs(interceptor.Random);
        executor.TerminalStateRandom.ShouldNotBeSameAs(random);
    }

    [Fact]
    public async Task TerminalState_IsYieldedOnceBeforeCompletion()
    {
        var executor = new ProbeExecutor { TerminalAtValue = 1 };

        var states = await Collect(executor, ct: TestContext.Current.CancellationToken);

        states.Select(state => state.Value).ShouldBe([1]);
        executor.StepCalls.ShouldBe(1);
    }

    [Fact]
    public async Task InitialState_IsPassedAsThePreviousState()
    {
        var initialState = new ProbeState(41);
        var executor = new ProbeExecutor { TerminalAtValue = 42 };

        var states = await Collect(executor, initialState: initialState, ct: TestContext.Current.CancellationToken);

        states.Select(state => state.Value).ShouldBe([42]);
        executor.FirstPreviousState.ShouldBeSameAs(initialState);
    }

    private static async Task<List<ProbeState>> Collect(ProbeExecutor executor, IRandomNumberGenerator? random = null, ProbeState? initialState = null, CancellationToken ct = default)
    {
        var problem = FuncProblem.Create((int value) => value, DummySearchSpace<int>.Instance, SingleObjective.Minimize);
        var states = new List<ProbeState>();
        await foreach (var state in executor.RunStreamingAsync(problem, random ?? new RecordingRandom(), initialState, ct))
        {
            states.Add(state);
        }

        return states;
    }

    private sealed record ProbeState(int Value) : ISearchState;

    private sealed class ProbeExecutor(IInterceptionExecutor<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, ProbeState>? interceptor = null)
        : IterativeAlgorithmExecutor<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, ProbeState>(interceptor)
    {
        public int? CompleteAtCount { get; init; }
        public int? TerminalAtValue { get; init; }
        public bool StopBeforeFirstStep { get; init; }
        public int StepCalls { get; private set; }
        public ProbeState? FirstPreviousState { get; private set; }
        public IRandomNumberGenerator? TerminalStateRandom { get; private set; }
        public List<string> Events { get; } = [];

        protected override ProbeState ExecuteStep(ProbeState? previousState, FuncProblem<int, DummySearchSpace<int>> problem, IRandomNumberGenerator random)
        {
            Events.Add("step");
            StepCalls++;
            FirstPreviousState ??= previousState;
            return new ProbeState((previousState?.Value ?? 0) + 1);
        }

        protected override bool TryExecuteStep(ProbeState? previousState, FuncProblem<int, DummySearchSpace<int>> problem, IRandomNumberGenerator random, [NotNullWhen(true)] out ProbeState? nextState)
        {
            if (StopBeforeFirstStep)
            {
                nextState = null;
                return false;
            }

            nextState = ExecuteStep(previousState, problem, random);
            return true;
        }

        protected override bool HasCompleted(int yieldedStateCount, ProbeState? previousState, FuncProblem<int, DummySearchSpace<int>> problem)
        {
            Events.Add($"complete:{yieldedStateCount}");
            return yieldedStateCount == CompleteAtCount;
        }

        protected override bool IsTerminalState(ProbeState state, int yieldedStateCount, ProbeState? previousState, FuncProblem<int, DummySearchSpace<int>> problem, IRandomNumberGenerator random)
        {
            Events.Add($"terminal:{state.Value}");
            TerminalStateRandom = random;
            return state.Value == TerminalAtValue;
        }
    }

    private sealed class RecordingInterceptor : IInterceptionExecutor<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, ProbeState>
    {
        public int Invocations { get; private set; }
        public IRandomNumberGenerator? Random { get; private set; }

        public ProbeState Transform(ProbeState currentState, ProbeState? previousState, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace, FuncProblem<int, DummySearchSpace<int>> problem)
        {
            Invocations++;
            Random = random;
            return currentState with { Value = currentState.Value + 1 };
        }
    }

    private sealed class RecordingRandom : IRandomNumberGenerator
    {
        public List<ulong> ForkKeys { get; } = [];

        public double NextDouble() => 0;

        public int NextInt() => 0;

        public IRandomNumberGenerator Fork(ulong forkKey)
        {
            ForkKeys.Add(forkKey);
            return this;
        }
    }
}
