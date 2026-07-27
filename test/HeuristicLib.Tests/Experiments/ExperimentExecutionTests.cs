using HEAL.HeuristicLib.Experiments;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.States;
using HEAL.HeuristicLib.Tests.Experiments.TestSupport;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Experiments;

public class ExperimentExecutionTests
{
    [Fact]
    public async Task SequentialPolicy_StreamsTrialsInMaterializationOrder()
    {
        var experiment = CreateExperiment(new ProbeAlgorithm(10), new ProbeAlgorithm(20), new ProbeAlgorithm(30));
        var run = ExperimentTestSupport.CreateRun(experiment);

        var entries = await run.Stream(ExperimentExecutionPolicy.Sequential(), cancellationToken: TestContext.Current.CancellationToken)
            .ToListAsync(TestContext.Current.CancellationToken);

        entries.Select(entry => entry.Trial.Key).ShouldBe([0, 1, 2]);
        entries.Select(entry => MetaAlgorithmTestHelpers.StateCandidate(entry.State)).ShouldBe([10, 20, 30]);
    }

    [Fact]
    public async Task BoundedConcurrentPolicy_LimitsActiveTrials()
    {
        var probe = new ExecutionProbe();
        var algorithm = new ProbeAlgorithm(1, probe, DelayMilliseconds: 30);
        var experiment = algorithm.Repeat(6);
        var run = experiment.CreateRun(MetaAlgorithmTestHelpers.CreateIntegerProblem(), RandomNumberGenerator.Create(42));

        _ = await run.CompleteAsync(ExperimentExecutionPolicy.Concurrent(2), cancellationToken: TestContext.Current.CancellationToken);

        probe.SetupCount.ShouldBe(6);
        probe.ExecutionCount.ShouldBe(6);
        probe.MaximumActiveExecutions.ShouldBe(2);
    }

    [Fact]
    public async Task ConcurrentPolicy_AllowsAllTrialsToBecomeActive()
    {
        var probe = new ExecutionProbe();
        var experiment = CreateExperiment(
            new ProbeAlgorithm(1, probe, DelayMilliseconds: 30),
            new ProbeAlgorithm(2, probe, DelayMilliseconds: 30),
            new ProbeAlgorithm(3, probe, DelayMilliseconds: 30),
            new ProbeAlgorithm(4, probe, DelayMilliseconds: 30));
        var run = ExperimentTestSupport.CreateRun(experiment);

        _ = await run.CompleteAsync(ExperimentExecutionPolicy.Concurrent(), cancellationToken: TestContext.Current.CancellationToken);

        probe.MaximumActiveExecutions.ShouldBe(4);
    }

    [Fact]
    public async Task RandomAssignments_DoNotDependOnSchedulingPolicy()
    {
        var algorithm = new ProbeAlgorithm(0, UseRandomValue: true);
        var experiment = algorithm.Repeat(5);
        var sequentialRun = experiment.CreateRun(MetaAlgorithmTestHelpers.CreateIntegerProblem(), RandomNumberGenerator.Create(123));
        var concurrentRun = experiment.CreateRun(MetaAlgorithmTestHelpers.CreateIntegerProblem(), RandomNumberGenerator.Create(123));

        var sequential = await sequentialRun.CompleteAsync(ExperimentExecutionPolicy.Sequential(), cancellationToken: TestContext.Current.CancellationToken);
        var concurrent = await concurrentRun.CompleteAsync(ExperimentExecutionPolicy.Concurrent(2), cancellationToken: TestContext.Current.CancellationToken);

        sequential.Select(result => (result.Trial.Key, MetaAlgorithmTestHelpers.StateCandidate(result.State)))
            .ShouldBe(concurrent.Select(result => (result.Trial.Key, MetaAlgorithmTestHelpers.StateCandidate(result.State))));
    }

    [Fact]
    public async Task Cancellation_StopsAllActiveConcurrentTrials()
    {
        var probe = new ExecutionProbe();
        var experiment = CreateExperiment(
            new ProbeAlgorithm(1, probe, DelayMilliseconds: 1_000),
            new ProbeAlgorithm(2, probe, DelayMilliseconds: 1_000),
            new ProbeAlgorithm(3, probe, DelayMilliseconds: 1_000));
        var run = ExperimentTestSupport.CreateRun(experiment);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cancellation.CancelAfter(TimeSpan.FromMilliseconds(30));

        await Should.ThrowAsync<OperationCanceledException>(() =>
            run.CompleteAsync(ExperimentExecutionPolicy.Concurrent(), cancellationToken: cancellation.Token));

        probe.ExecutionCount.ShouldBe(3);
        probe.ActiveExecutions.ShouldBe(0);
    }

    [Fact]
    public async Task CombinedExecution_AggregatesFailuresInMaterializationOrderAndContinuesUnaffectedTrials()
    {
        var probe = new ExecutionProbe();
        var experiment = CreateExperiment(
            new ProbeAlgorithm(0, probe, FailDuringExecution: true),
            new ProbeAlgorithm(1, probe),
            new ProbeAlgorithm(2, probe, FailDuringSetup: true));
        var run = ExperimentTestSupport.CreateRun(experiment);

        var exception = await Should.ThrowAsync<AggregateException>(() =>
            run.CompleteAsync(ExperimentExecutionPolicy.Concurrent(2), cancellationToken: TestContext.Current.CancellationToken));

        exception.InnerExceptions.Cast<ExperimentTrialException<int>>().Select(failure => failure.Key).ShouldBe([0, 2]);
        probe.Candidates.ShouldBe([1]);
        probe.SetupCount.ShouldBe(3);
        probe.ExecutionCount.ShouldBe(2);
    }

    [Fact]
    public async Task Complete_ReportsTrialsThatYieldNoState()
    {
        var experiment = CreateExperiment(new ProbeAlgorithm(0, YieldState: false), new ProbeAlgorithm(1));
        var run = ExperimentTestSupport.CreateRun(experiment);

        var exception = await Should.ThrowAsync<AggregateException>(() =>
            run.CompleteAsync(ExperimentExecutionPolicy.Sequential(), cancellationToken: TestContext.Current.CancellationToken));

        var trialException = exception.InnerExceptions.Single().ShouldBeOfType<ExperimentTrialException<int>>();
        trialException.Key.ShouldBe(0);
        trialException.InnerException.ShouldBeOfType<InvalidOperationException>();
    }

    private static FixedExperiment<ProbeAlgorithm> CreateExperiment(params IReadOnlyList<ProbeAlgorithm> algorithms) =>
        new([.. algorithms.Select((algorithm, index) => new ExperimentCase<ProbeAlgorithm, int>(algorithm, index, [index]))]);
}
