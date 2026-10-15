using HEAL.HeuristicLib.Experiments;
using HEAL.HeuristicLib.Problems;
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

        run.LifecycleState.ShouldBe(RunLifecycleState.Preparing);
        _ = run.Stream(ExecutionConcurrency.Concurrent(2), cancellationToken: TestContext.Current.CancellationToken);

        run.LifecycleState.ShouldBe(RunLifecycleState.Running);
        run.Trials.ShouldAllBe(trial => trial.Run.LifecycleState == RunLifecycleState.Running);
        Should.Throw<InvalidOperationException>(() => run.Trials[0].Run.Stream(cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public void CombinedExecution_CreatesExecutionsBeforeEnumeration()
    {
        var evaluator = new CountingResolutionEvaluator();
        var algorithm = new CountingExecutionAlgorithm(1, evaluator);
        var experiment = algorithm.Repeat(2);
        var run = experiment.CreateRun(MetaAlgorithmTestHelpers.CreateIntegerProblem(), RandomNumberGenerator.Create(42));

        _ = run.Stream(cancellationToken: TestContext.Current.CancellationToken);

        algorithm.ExecutionCount.ShouldBe(2);
        evaluator.ExecutionCount.ShouldBe(2);
    }

    [Fact]
    public void StartTrials_StartsAndConsumesTheExperimentRun()
    {
        var run = CreateRun();

        _ = run.StartTrials(cancellationToken: TestContext.Current.CancellationToken);

        run.LifecycleState.ShouldNotBe(RunLifecycleState.Preparing);
        run.Trials.ShouldAllBe(trial => trial.Run.LifecycleState != RunLifecycleState.Preparing);
        Should.Throw<InvalidOperationException>(() => run.StartTrials(cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public void IndividualExecution_PreventsCombinedExecutionButLeavesOtherTrialsAvailable()
    {
        var run = CreateRun();

        _ = run.Trials[0].Run.Stream(cancellationToken: TestContext.Current.CancellationToken);

        run.LifecycleState.ShouldBe(RunLifecycleState.Preparing);
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

        run.LifecycleState.ShouldBe(RunLifecycleState.Canceled);
        Should.Throw<InvalidOperationException>(() => run.Stream(cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CompleteAsync_ReportsCompletedLifecycle()
    {
        var run = CreateRun();

        _ = await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        run.LifecycleState.ShouldBe(RunLifecycleState.Completed);
        run.Trials.ShouldAllBe(trial => trial.Run.LifecycleState == RunLifecycleState.Completed);
    }

    [Fact]
    public async Task DisposingStreamEarly_ReportsStoppedLifecycle()
    {
        var run = CreateRun();

        await using (var stream = run.Stream(cancellationToken: TestContext.Current.CancellationToken)
            .GetAsyncEnumerator(TestContext.Current.CancellationToken))
            (await stream.MoveNextAsync()).ShouldBeTrue();

        run.LifecycleState.ShouldBe(RunLifecycleState.Stopped);
    }

    private static RepeatedExperiment<int, AdditiveStepAlgorithm, PopulationState<int>> CreateExperiment() =>
        new AdditiveStepAlgorithm(1).Repeat(2);

    private static ExperimentRun<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>, AdditiveStepAlgorithm, int> CreateRun() =>
        CreateExperiment().CreateRun(MetaAlgorithmTestHelpers.CreateIntegerProblem(), RandomNumberGenerator.Create(42));
}
