using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
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
    public void ABroadTerminator_SharesItsScopedStateAcrossCompatibleStateViews(bool secondViewFirst)
    {
        var probe = new Probe();
        var terminator = new ThirdCallTerminator(probe);
        var root = ResolutionScope.Create();
        ITerminationExecutor<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, FirstState> first;
        ITerminationExecutor<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, SecondState> second;
        if (secondViewFirst)
        {
            second = root.For<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, SecondState>().Resolve(terminator);
            first = root.For<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, FirstState>().Resolve(terminator);
        }
        else
        {
            first = root.For<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, FirstState>().Resolve(terminator);
            second = root.For<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, SecondState>().Resolve(terminator);
        }
        var problem = CreateProblem();

        first.IsTerminalState(new FirstState(), RandomNumberGenerator.Create(0), problem.SearchSpace, problem).ShouldBeFalse();
        second.IsTerminalState(new SecondState(), RandomNumberGenerator.Create(0), problem.SearchSpace, problem).ShouldBeFalse();
        first.IsTerminalState(new FirstState(), RandomNumberGenerator.Create(0), problem.SearchSpace, problem).ShouldBeTrue();

        probe.Preparations.ShouldBe(1);
    }

    [Fact]
    public void GeneralAdvice_WrapsTerminationAndInterception()
    {
        var total = new CallCounter();
        var root = ResolutionScope.Create(Aspect.Create(Pointcut.OfType<IOperator>(), new CountCalls(total, new AdviceLog())));
        var scope = root.For<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, FirstState>();
        var terminator = scope.Resolve(new ThirdCallTerminator(new Probe()));
        var interceptor = scope.Resolve(new CountingInterceptor(new Probe()));
        var problem = CreateProblem();

        terminator.IsTerminalState(new FirstState(), RandomNumberGenerator.Create(0), problem.SearchSpace, problem).ShouldBeFalse();
        interceptor.Transform(new FirstState(), null, RandomNumberGenerator.Create(0), problem.SearchSpace, problem);

        total.Value.ShouldBe(2);
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

    private sealed class CallCount
    {
        public int Calls { get; set; }
    }

    private sealed record FirstState : SearchState;

    private sealed record SecondState : SearchState;

    private sealed record ThirdCallTerminator(Probe Probe) : StatefulTerminator<int, CallCount>
    {
        protected override CallCount CreateScopedState()
        {
            Probe.Preparations++;
            return new CallCount();
        }

        protected override bool IsTerminalState(CallCount state) => ++state.Calls >= 3;
    }

    private sealed record CountingInterceptor(Probe Probe) : StatefulInterceptor<int, FirstState, CallCount>
    {
        protected override CallCount CreateScopedState()
        {
            Probe.Preparations++;
            return new CallCount();
        }

        protected override FirstState Transform(FirstState currentState, FirstState? previousState, CallCount operatorState, IRandomNumberGenerator random)
        {
            operatorState.Calls++;
            return currentState;
        }
    }
}
