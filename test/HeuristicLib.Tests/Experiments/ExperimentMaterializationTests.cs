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
        var experiment = new AdditiveStepAlgorithm(0)
            .AsGrid()
            .VaryBy([1, 2], (algorithm, increment) => algorithm with { Increment = increment })
            .Repeat(2);

        var cases = experiment.MaterializeCases();

        cases.Select(experimentCase => (experimentCase.Key.Inner.Increment, experimentCase.Key.Repetition)).ShouldBe([(1, 0), (1, 1), (2, 0), (2, 1)]);
        cases.Select(experimentCase => experimentCase.RandomForkPath.ToArray()).ShouldBe([[0, 0], [0, 1], [1, 0], [1, 1]]);
    }

    [Fact]
    public void Grid_RejectsEqualMaterializedConfigurations()
    {
        var experiment = new AdditiveStepAlgorithm(0)
            .AsGrid()
            .VaryBy([1, 1], (algorithm, increment) => algorithm with { Increment = increment });

        Should.Throw<InvalidOperationException>(() => experiment.MaterializeCases());
    }

    [Fact]
    public void Grid_CapturesAnImmutableSnapshotOfDimensionValues()
    {
        var increments = new List<int> { 1, 2 };
        var experiment = new AdditiveStepAlgorithm(0)
            .AsGrid()
            .VaryBy(increments, (algorithm, increment) => algorithm with { Increment = increment });

        increments[0] = 3;

        experiment.MaterializeCases().Select(experimentCase => experimentCase.Algorithm.Increment).ShouldBe([1, 2]);
    }

    [Fact]
    public void ExperimentRun_RejectsDuplicateKeys()
    {
        var algorithm = new ProbeAlgorithm(1);
        var experiment = new FixedExperiment<ProbeAlgorithm>([
            ExperimentCase.From(algorithm, 0, [0]),
            ExperimentCase.From(algorithm, 0, [1])
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
