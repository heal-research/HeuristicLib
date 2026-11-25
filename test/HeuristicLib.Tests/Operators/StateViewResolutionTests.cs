using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests.ExecutionInfrastructure.OperatorFoundation;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Operators;

/// <summary>
/// Termination and interception run over the run's search state. A terminator only reads the state, so one written
/// for a broad state serves every narrower view and shares its prepared data across them. An interceptor returns the
/// state, so it serves only the state it is written for.
/// </summary>
public class StateViewResolutionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ABroadTerminator_SharesItsPrivateFieldsAcrossTheCompatibleStateViewsOfAScope(bool secondViewFirst)
    {
        var probe = new Probe();
        var terminator = new ThirdCallTerminator(probe);
        var root = ResolutionScope.Create();
        var problem = CreateProblem();
        ITerminationExecutor<FirstState> first;
        ITerminationExecutor<SecondState> second;
        if (secondViewFirst)
        {
            second = root.For<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, SecondState>(problem).Resolve(terminator);
            first = root.For<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, FirstState>(problem).Resolve(terminator);
        }
        else
        {
            first = root.For<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, FirstState>(problem).Resolve(terminator);
            second = root.For<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, SecondState>(problem).Resolve(terminator);
        }

        first.IsTerminalState(new FirstState(), RandomNumberGenerator.Create(0)).ShouldBeFalse();
        second.IsTerminalState(new SecondState(), RandomNumberGenerator.Create(0)).ShouldBeFalse();
        first.IsTerminalState(new FirstState(), RandomNumberGenerator.Create(0)).ShouldBeTrue();

        probe.Preparations.ShouldBe(1);
    }

    [Fact]
    public void AGeneralWrapper_WrapsTerminationAndInterception()
    {
        var calls = new Counter();
        var root = ResolutionScope.Create();
        var scope = root.For<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, FirstState>(CreateProblem());
        var terminator = scope.Resolve(new ThirdCallTerminator(new Probe()).Counted(calls));
        var interceptor = scope.Resolve(new CountingInterceptor(new Probe()).Counted(calls));

        terminator.IsTerminalState(new FirstState(), RandomNumberGenerator.Create(0)).ShouldBeFalse();
        interceptor.Transform(new FirstState(), null, RandomNumberGenerator.Create(0));

        root.Results.Result(calls).CurrentCount.ShouldBe(2);
    }

    private static FuncProblem<int, DummySearchSpace<int>> CreateProblem()
    {
        return FuncProblem.Create(
            evaluateFunc: static (int candidate) => candidate,
            encoding: DummySearchSpace<int>.Instance,
            objective: SingleObjective.Minimize);
    }

    private sealed class Probe
    {
        public int Preparations { get; set; }
    }

    private sealed record FirstState : SearchState;

    private sealed record SecondState : SearchState;

    private sealed record ThirdCallTerminator(Probe Probe) : Terminator<int>
    {

        protected override ITerminationExecutor<ISearchState> CreateExecutor(ResolutionScope<int, ISearchSpace<int>, IProblem<int, ISearchSpace<int>>, ISearchState> scope)
        {
            Probe.Preparations++;
            return new Executor();
        }

        private sealed class Executor
            : TerminationExecutor
        {
            private int invocations;

            public override bool IsTerminalState() => ++invocations >= 3;
        }
    }

    private sealed record CountingInterceptor(Probe Probe) : Interceptor<int, FirstState>
    {

        protected override IInterceptionExecutor<FirstState> CreateExecutor(ResolutionScope<int, ISearchSpace<int>, IProblem<int, ISearchSpace<int>>, FirstState> scope)
        {
            Probe.Preparations++;
            return new Executor();
        }

        private sealed class Executor
            : InterceptionExecutor<FirstState>
        {
            private int invocations;

            public override FirstState Transform(FirstState currentState, FirstState? previousState, IRandomNumberGenerator random)
            {
                invocations++;
                return currentState;
            }
        }
    }
}
