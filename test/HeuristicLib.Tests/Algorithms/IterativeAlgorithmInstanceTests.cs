using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.States;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Algorithms;

public class IterativeAlgorithmInstanceTests
{
    [Fact]
    public async Task HasCompleted_IsCheckedBeforeExecutingAStep()
    {
        var instance = new ProbeInstance { CompleteAtCount = 0 };

        var states = await Collect(instance, ct: TestContext.Current.CancellationToken);

        states.ShouldBeEmpty();
        instance.StepCalls.ShouldBe(0);
        instance.Events.ShouldBe(["complete:0"]);
    }

    [Fact]
    public async Task Cancellation_IsCheckedBeforeExecutingAStep()
    {
        var instance = new ProbeInstance();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(async () => await Collect(instance, ct: cts.Token));

        instance.StepCalls.ShouldBe(0);
    }

    [Fact]
    public async Task RandomForks_UseTheYieldedStateCount()
    {
        var instance = new ProbeInstance { CompleteAtCount = 2 };
        var random = new RecordingRandom();

        _ = await Collect(instance, random, ct: TestContext.Current.CancellationToken);

        random.ForkKeys.ShouldBe([0UL, 1UL]);
    }

    [Fact]
    public async Task TryExecuteStepReturningFalse_DoesNotYieldOrIntercept()
    {
        var interceptor = new RecordingInterceptor();
        var instance = new ProbeInstance(interceptor) { StopBeforeFirstStep = true };

        var states = await Collect(instance, ct: TestContext.Current.CancellationToken);

        states.ShouldBeEmpty();
        interceptor.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task Interception_PrecedesTerminalStateEvaluation()
    {
        var interceptor = new RecordingInterceptor();
        var instance = new ProbeInstance(interceptor) { TerminalAtValue = 2 };

        var states = await Collect(instance, ct: TestContext.Current.CancellationToken);

        states.Select(state => state.Value).ShouldBe([2]);
        instance.Events.ShouldBe(["complete:0", "step", "terminal:2"]);
    }

    [Fact]
    public async Task TerminalState_IsYieldedOnceBeforeCompletion()
    {
        var instance = new ProbeInstance { TerminalAtValue = 1 };

        var states = await Collect(instance, ct: TestContext.Current.CancellationToken);

        states.Select(state => state.Value).ShouldBe([1]);
        instance.StepCalls.ShouldBe(1);
    }

    [Fact]
    public async Task InitialState_IsPassedAsThePreviousState()
    {
        var initialState = new ProbeState(41);
        var instance = new ProbeInstance { TerminalAtValue = 42 };

        var states = await Collect(instance, initialState: initialState, ct: TestContext.Current.CancellationToken);

        states.Select(state => state.Value).ShouldBe([42]);
        instance.FirstPreviousState.ShouldBeSameAs(initialState);
    }

    private static async Task<List<ProbeState>> Collect(ProbeInstance instance, IRandomNumberGenerator? random = null, ProbeState? initialState = null, CancellationToken ct = default)
    {
        var problem = FuncProblem.Create((int value) => value, DummySearchSpace<int>.Instance, SingleObjective.Minimize);
        var states = new List<ProbeState>();
        await foreach (var state in instance.RunStreamingAsync(problem, random ?? new RecordingRandom(), initialState, ct))
        {
            states.Add(state);
        }

        return states;
    }

    private sealed record ProbeState(int Value) : ISearchState;

    private sealed class ProbeInstance(IInterceptorInstance<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, ProbeState>? interceptor = null)
        : IterativeAlgorithmInstance<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, ProbeState>(interceptor)
    {
        public int? CompleteAtCount { get; init; }
        public int? TerminalAtValue { get; init; }
        public bool StopBeforeFirstStep { get; init; }
        public int StepCalls { get; private set; }
        public ProbeState? FirstPreviousState { get; private set; }
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

        protected override bool IsTerminalState(ProbeState state, int yieldedStateCount, ProbeState? previousState, FuncProblem<int, DummySearchSpace<int>> problem)
        {
            Events.Add($"terminal:{state.Value}");
            return state.Value == TerminalAtValue;
        }
    }

    private sealed class RecordingInterceptor : IInterceptorInstance<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, ProbeState>
    {
        public int Calls { get; private set; }

        public ProbeState Transform(ProbeState currentState, ProbeState? previousState, DummySearchSpace<int> searchSpace, FuncProblem<int, DummySearchSpace<int>> problem)
        {
            Calls++;
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
