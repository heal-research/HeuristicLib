using HEAL.HeuristicLib.Experiments;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.States;
using HEAL.HeuristicLib.Tests.Experiments.TestSupport;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Experiments;

public class ExperimentMaterializationTests
{
    [Fact]
    public void GridAndRepetition_MaterializeTypedKeysAndImmutableForkPaths()
    {
        var grid = new GridExperiment<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>, AdditiveStepAlgorithm>(new AdditiveStepAlgorithm(0))
            .VaryBy([1, 2], (algorithm, increment) => algorithm with { Increment = increment });
        var experiment = new RepeatedExperiment<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>, AdditiveStepAlgorithm, AdditiveStepAlgorithm>(grid, repetitions: 2);

        var cases = experiment.MaterializeCases();

        cases.Select(experimentCase => (experimentCase.Key.Inner.Increment, experimentCase.Key.Repetition)).ShouldBe([(1, 0), (1, 1), (2, 0), (2, 1)]);
        cases.Select(experimentCase => experimentCase.RandomForkPath.ToArray()).ShouldBe([[0, 0], [0, 1], [1, 0], [1, 1]]);
    }

    [Fact]
    public void Grid_RejectsEqualMaterializedConfigurations()
    {
        var experiment = new GridExperiment<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>, AdditiveStepAlgorithm>(new AdditiveStepAlgorithm(0))
            .VaryBy([1, 1], (algorithm, increment) => algorithm with { Increment = increment });

        Should.Throw<InvalidOperationException>(() => experiment.MaterializeCases());
    }

    [Fact]
    public void ExperimentRun_RejectsDuplicateKeys()
    {
        var algorithm = new ProbeAlgorithm(1);
        var experiment = new FixedExperiment<ProbeAlgorithm>([
            new ExperimentCase<ProbeAlgorithm, int>(algorithm, 0, [0]),
            new ExperimentCase<ProbeAlgorithm, int>(algorithm, 0, [1])
        ]);

        Should.Throw<InvalidOperationException>(() => ExperimentTestSupport.CreateRun(experiment));
    }

    [Fact]
    public void ExperimentRun_RejectsNoMaterializedCases()
    {
        var experiment = new FixedExperiment<ProbeAlgorithm>([]);

        Should.Throw<InvalidOperationException>(() => ExperimentTestSupport.CreateRun(experiment));
    }
}
