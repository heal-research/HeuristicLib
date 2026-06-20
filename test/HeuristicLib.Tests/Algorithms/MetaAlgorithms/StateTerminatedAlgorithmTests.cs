using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.MetaAlgorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.States;
using HEAL.HeuristicLib.Tests.TestSupport.Execution;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Algorithms.MetaAlgorithms;

public class StateTerminatedAlgorithmTests
{
    [Fact]
    public void RunStreaming_DoesNotCheckSuppliedInitialState()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var terminator = new RecordingTerminator(_ => false);
        var algorithm = CreateStateTerminatedAlgorithm(terminator);
        var initialState = CreateState(41);

        var states = algorithm.RunStreaming(
          problem,
          RandomNumberGenerator.Create(42),
          initialState,
          TestContext.Current.CancellationToken).ToList();

        states.Select(MetaAlgorithmTestHelpers.StateGenotype).ShouldBe([42]);
        terminator.CheckedGenotypes.ShouldBe([42]);
    }

    [Fact]
    public void RunStreaming_YieldsFirstProducedStateBeforeCheckingTerminator()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var terminator = new RecordingTerminator(_ => true);
        var algorithm = CreateStateTerminatedAlgorithm(terminator);
        var initialState = CreateState(41);

        var states = algorithm.RunStreaming(
          problem,
          RandomNumberGenerator.Create(42),
          initialState,
          TestContext.Current.CancellationToken).ToList();

        states.Select(MetaAlgorithmTestHelpers.StateGenotype).ShouldBe([42]);
        terminator.CheckedGenotypes.ShouldBe([42]);
    }

    [Fact]
    public void RunStreaming_TerminatorThatStopsImmediatelyStillIncludesTriggeringState()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var terminator = new RecordingTerminator(_ => true);
        var algorithm = CreateStateTerminatedAlgorithm(terminator);

        var states = algorithm.RunStreaming(
          problem,
          RandomNumberGenerator.Create(42),
          ct: TestContext.Current.CancellationToken).ToList();

        states.Select(MetaAlgorithmTestHelpers.StateGenotype).ShouldBe([1]);
        terminator.CheckedGenotypes.ShouldBe([1]);
    }

    [Fact]
    public void WithMaxIterations_YieldsTriggeringStateBeforeStopping()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var algorithm = new AdditiveStepAlgorithm(1).WithMaxIterations(1);

        var states = algorithm.RunStreaming(
          problem,
          RandomNumberGenerator.Create(42),
          ct: TestContext.Current.CancellationToken).ToList();

        states.Select(MetaAlgorithmTestHelpers.StateGenotype).ShouldBe([1]);
    }

    [Fact]
    public void WithMaxIterations_Throws_WhenMaximumIterationsIsNotPositive()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
          new AdditiveStepAlgorithm(1).WithMaxIterations(0));
    }

    [Fact]
    public void RunStreaming_WithCanceledRunToken_InterruptsBeforeProducingState()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Should.Throw<OperationCanceledException>(() =>
            new AdditiveStepAlgorithm(1).RunStreaming(
              problem,
              RandomNumberGenerator.Create(42),
              ct: cts.Token).ToList());
    }

    [Fact]
    public void RunStreaming_WithCanceledTerminatorToken_YieldsProducedStateThenStops()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var algorithm = CreateStateTerminatedAlgorithm(new CancellationTokenTerminator<int>(cts.Token));

        var states = algorithm.RunStreaming(
          problem,
          RandomNumberGenerator.Create(42),
          ct: TestContext.Current.CancellationToken).ToList();

        states.Select(MetaAlgorithmTestHelpers.StateGenotype).ShouldBe([1]);
    }

    [Fact]
    public void RunStreaming_WithTerminatorTokenCanceledImmediatelyBeforeRun_YieldsProducedStateThenStops()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        using var cts = new CancellationTokenSource();
        var algorithm = CreateStateTerminatedAlgorithm(new CancellationTokenTerminator<int>(cts.Token));

        cts.Cancel();
        var states = algorithm.RunStreaming(
          problem,
          RandomNumberGenerator.Create(42),
          ct: TestContext.Current.CancellationToken).ToList();

        states.Select(MetaAlgorithmTestHelpers.StateGenotype).ShouldBe([1]);
    }

    [Fact]
    public void AfterElapsedTimeTerminator_UsesElapsedTimeFromExecutionInstanceCreation()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var timeProvider = new ManualTimeProvider();
        var terminator = new AfterElapsedTimeTerminator<int>(TimeSpan.FromSeconds(5), timeProvider);
        var instance = terminator.CreateExecutionInstance(new ExecutionInstanceRegistry(TestRun.Instance));

        instance.IsTerminalState(CreateState(1), problem.SearchSpace, problem).ShouldBeFalse();

        timeProvider.Advance(TimeSpan.FromSeconds(5));

        instance.IsTerminalState(CreateState(2), problem.SearchSpace, problem).ShouldBeTrue();
    }

    [Fact]
    public void AfterElapsedTimeTerminator_Throws_WhenMaximumElapsedTimeIsNotPositive()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
          new AfterElapsedTimeTerminator<int>(TimeSpan.Zero));
    }

    [Fact]
    public void CreateExecutionInstance_ResolvesTerminatorBeforeWrappedAlgorithm()
    {
        var events = new List<string>();
        var algorithm = new StateTerminatedAlgorithm<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>
        {
            Algorithm = new RecordingAlgorithm(events),
            Terminator = new RecordingResolveTerminator(events)
        };

        _ = algorithm.CreateExecutionInstance(TestRun.Instance);

        events.ShouldBe(["terminator", "algorithm"]);
    }

    private static StateTerminatedAlgorithm<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>> CreateStateTerminatedAlgorithm(
      ITerminator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>> terminator)
    {
        return new StateTerminatedAlgorithm<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>
        {
            Algorithm = new AdditiveStepAlgorithm(1),
            Terminator = terminator
        };
    }

    private static PopulationState<int> CreateState(int genotype)
    {
        return new PopulationState<int>
        {
            Population = Population.From([Solution.From(genotype, genotype)])
        };
    }

    private sealed record RecordingTerminator(Func<int, bool> ShouldStop)
      : StatelessTerminator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>
    {
        public List<int> CheckedGenotypes { get; } = [];

        public override bool IsTerminalState(
          PopulationState<int> state,
          DummySearchSpace<int> searchSpace,
          IProblem<int, DummySearchSpace<int>> problem)
        {
            var genotype = MetaAlgorithmTestHelpers.StateGenotype(state);
            CheckedGenotypes.Add(genotype);
            return ShouldStop(genotype);
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
      : Algorithm<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>, RecordingAlgorithm.ExecutionState>
    {
        public new sealed class ExecutionState
          : Algorithm<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>, ExecutionState>.ExecutionState;

        protected override ExecutionState CreateInitialExecutionState(IExecutionInstanceResolver resolver)
        {
            Events.Add("algorithm");
            return new ExecutionState
            {
                Evaluator = resolver.Resolve(Evaluator)
            };
        }

        protected override IAlgorithmInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>> CreateAlgorithmInstance(
          Run run,
          ExecutionState executionState)
        {
            return new Instance(run, executionState.Evaluator);
        }

        private sealed class Instance(
          Run run,
          IEvaluatorInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>> evaluator)
          : AlgorithmInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>(run, evaluator)
        {
            public override async IAsyncEnumerable<PopulationState<int>> RunStreamingAsync(
              IProblem<int, DummySearchSpace<int>> problem,
              IRandomNumberGenerator random,
              PopulationState<int>? initialState = null,
              [System.Runtime.CompilerServices.EnumeratorCancellation]
              CancellationToken ct = default)
            {
                await Task.CompletedTask;
                yield break;
            }
        }
    }

    private sealed record RecordingResolveTerminator(List<string> Events)
      : ITerminator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>
    {
        public ITerminatorInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>> CreateExecutionInstance(
          ExecutionInstanceRegistry instanceRegistry)
        {
            Events.Add("terminator");
            return new Instance();
        }

        private sealed class Instance : ITerminatorInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>
        {
            public bool IsTerminalState(
              PopulationState<int> state,
              DummySearchSpace<int> searchSpace,
              IProblem<int, DummySearchSpace<int>> problem)
            {
                return false;
            }
        }
    }
}
