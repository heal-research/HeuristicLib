using HEAL.HeuristicLib.Experiments;
using HEAL.HeuristicLib.Random;
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
    public void Grid_VaryByImmediatelyMaterializesANewStructurallyComparableGrid()
    {
        var calls = 0;
        var original = Grid.Create(new AdditiveStepAlgorithm(0));
        var varied = original.VaryBy([1, 2], (algorithm, increment) =>
        {
            calls++;
            return algorithm with { Increment = increment };
        });

        calls.ShouldBe(2);
        original.Configurations.ShouldBe([new AdditiveStepAlgorithm(0)]);
        varied.Configurations.ShouldBe([new AdditiveStepAlgorithm(1), new AdditiveStepAlgorithm(2)]);
        varied.ShouldBe(Grid.Create(new AdditiveStepAlgorithm(0))
            .VaryBy([1, 2], static (algorithm, increment) => algorithm with { Increment = increment }));

        _ = varied.GetConfigurations();
        calls.ShouldBe(2);
    }

    [Fact]
    public void Grid_EmptyDimensionProducesEmptyGrid()
    {
        var experiment = new AdditiveStepAlgorithm(0).AsGrid()
            .VaryBy(Array.Empty<int>(), static (algorithm, increment) => algorithm with { Increment = increment });

        experiment.ParameterGrid.Configurations.ShouldBeEmpty();
        experiment.MaterializeCases().ShouldBeEmpty();
        experiment.VaryBy([1, 2], static (algorithm, increment) => algorithm with { Increment = increment })
            .ParameterGrid.Configurations.ShouldBeEmpty();
        Should.Throw<InvalidOperationException>(() => experiment.CreateRun(
            MetaAlgorithmTestHelpers.CreateIntegerProblem(),
            RandomNumberGenerator.Create(42)));
    }

    [Fact]
    public void ExperimentCase_SnapshotsRandomForkPath()
    {
        var path = new List<int> { 1, 2 };
        var experimentCase = new ExperimentCase<AdditiveStepAlgorithm, int>(new AdditiveStepAlgorithm(1), 0, path);

        path[0] = 3;

        experimentCase.RandomForkPath.ShouldBe([1, 2]);
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
