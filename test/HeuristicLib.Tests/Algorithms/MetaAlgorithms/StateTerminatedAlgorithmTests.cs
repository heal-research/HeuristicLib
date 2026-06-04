using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.MetaAlgorithms;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.States;
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

    private static StateTerminatedAlgorithm<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>> CreateStateTerminatedAlgorithm(
      RecordingTerminator terminator)
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
}
