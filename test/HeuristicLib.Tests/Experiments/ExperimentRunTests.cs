using HEAL.HeuristicLib.Experiments;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.States;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Experiments;

public class ExperimentRunTests
{
    [Fact]
    public void MaterializeCases_ProvidesCasesForManualInspection()
    {
        var experiment = CreateExperiment();

        var cases = experiment.MaterializeCases();

        cases.Select(experimentCase => experimentCase.Key).ShouldBe([0, 1]);
        cases.Select(experimentCase => experimentCase.Algorithm).ShouldAllBe(algorithm => ReferenceEquals(algorithm, experiment.Algorithm));
    }

    [Fact]
    public void CombinedExecution_PreparesEveryTrialBeforeEnumeration()
    {
        var run = CreateRun();

        run.ExecutionStarted.ShouldBeFalse();
        _ = run.Stream(ExperimentExecutionPolicy.Concurrent(2), cancellationToken: TestContext.Current.CancellationToken);

        run.ExecutionStarted.ShouldBeTrue();
        run.Trials.ShouldAllBe(trial => trial.Run.ExecutionStarted);
        Should.Throw<InvalidOperationException>(() => run.Trials[0].Run.Stream(cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public void CombinedExecution_CreatesExecutionInstancesBeforeEnumeration()
    {
        var evaluator = new CountingResolutionEvaluator();
        var algorithm = new CountingInstanceAlgorithm(1, evaluator);
        var experiment = algorithm.Repeat(2);
        var run = experiment.CreateRun(MetaAlgorithmTestHelpers.CreateIntegerProblem(), RandomNumberGenerator.Create(42));

        _ = run.Stream(cancellationToken: TestContext.Current.CancellationToken);

        algorithm.InstanceCount.ShouldBe(2);
        evaluator.InstanceCount.ShouldBe(2);
    }

    [Fact]
    public void IndividualExecution_PreventsCombinedExecutionButLeavesOtherTrialsAvailable()
    {
        var run = CreateRun();

        _ = run.Trials[0].Run.Stream(cancellationToken: TestContext.Current.CancellationToken);

        run.ExecutionStarted.ShouldBeTrue();
        Should.Throw<InvalidOperationException>(() => run.Stream(cancellationToken: TestContext.Current.CancellationToken));
        _ = run.Trials[1].Run.Stream(cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    public void CombinedExecution_WithCancelledToken_ConsumesRun()
    {
        var run = CreateRun();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Should.Throw<OperationCanceledException>(() => run.Stream(cancellationToken: cancellation.Token));

        run.ExecutionStarted.ShouldBeTrue();
        Should.Throw<InvalidOperationException>(() => run.Stream(cancellationToken: TestContext.Current.CancellationToken));
    }

    private static RepeatedExperiment<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>, AdditiveStepAlgorithm> CreateExperiment() =>
        new AdditiveStepAlgorithm(1).Repeat(2);

    private static ExperimentRun<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>, AdditiveStepAlgorithm, int> CreateRun() =>
        CreateExperiment().CreateRun(MetaAlgorithmTestHelpers.CreateIntegerProblem(), RandomNumberGenerator.Create(42));
}
