using HEAL.HeuristicLib.Execution;
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
    public async Task SequentialExecution_StreamsTrialsInMaterializationOrder()
    {
        var experiment = CreateExperiment(new ProbeAlgorithm(10), new ProbeAlgorithm(20), new ProbeAlgorithm(30));
        var run = ExperimentTestSupport.CreateRun(experiment);

        var entries = await run.Stream(ExecutionConcurrency.Sequential(), cancellationToken: TestContext.Current.CancellationToken)
            .ToListAsync(TestContext.Current.CancellationToken);

        entries.Select(entry => entry.Trial.Key).ShouldBe([0, 1, 2]);
        entries.Select(entry => MetaAlgorithmTestHelpers.StateCandidate(entry.State)).ShouldBe([10, 20, 30]);
    }

    [Fact]
    public async Task BoundedConcurrency_LimitsActiveTrials()
    {
        var probe = new ExecutionProbe();
        var algorithm = new ProbeAlgorithm(1, probe, DelayMilliseconds: 30);
        var experiment = algorithm.Repeat(6);
        var run = experiment.CreateRun(MetaAlgorithmTestHelpers.CreateIntegerProblem(), RandomNumberGenerator.Create(42));

        _ = await run.CompleteAsync(ExecutionConcurrency.Concurrent(2), cancellationToken: TestContext.Current.CancellationToken);

        probe.SetupCount.ShouldBe(6);
        probe.ExecutionCount.ShouldBe(6);
        probe.MaximumActiveExecutions.ShouldBe(2);
    }

    [Fact]
    public async Task UnboundedConcurrency_AllowsAllTrialsToBecomeActive()
    {
        var probe = new ExecutionProbe();
        var experiment = CreateExperiment(
            new ProbeAlgorithm(1, probe, DelayMilliseconds: 30),
            new ProbeAlgorithm(2, probe, DelayMilliseconds: 30),
            new ProbeAlgorithm(3, probe, DelayMilliseconds: 30),
            new ProbeAlgorithm(4, probe, DelayMilliseconds: 30));
        var run = ExperimentTestSupport.CreateRun(experiment);

        _ = await run.CompleteAsync(ExecutionConcurrency.Concurrent(), cancellationToken: TestContext.Current.CancellationToken);

        probe.MaximumActiveExecutions.ShouldBe(4);
    }

    [Fact]
    public async Task RandomAssignments_DoNotDependOnExecutionConcurrency()
    {
        var algorithm = new ProbeAlgorithm(0, UseRandomValue: true);
        var experiment = algorithm.Repeat(5);
        var sequentialRun = experiment.CreateRun(MetaAlgorithmTestHelpers.CreateIntegerProblem(), RandomNumberGenerator.Create(123));
        var concurrentRun = experiment.CreateRun(MetaAlgorithmTestHelpers.CreateIntegerProblem(), RandomNumberGenerator.Create(123));

        var sequential = await sequentialRun.CompleteAsync(ExecutionConcurrency.Sequential(), cancellationToken: TestContext.Current.CancellationToken);
        var concurrent = await concurrentRun.CompleteAsync(ExecutionConcurrency.Concurrent(2), cancellationToken: TestContext.Current.CancellationToken);

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
            run.CompleteAsync(ExecutionConcurrency.Concurrent(), cancellationToken: cancellation.Token));

        probe.ExecutionCount.ShouldBe(3);
        probe.ActiveExecutions.ShouldBe(0);
    }

    [Fact]
    public async Task Cancellation_TakesPrecedenceOverTrialFailures()
    {
        var experiment = CreateExperiment(
            new ProbeAlgorithm(1, FailDuringExecution: true),
            new ProbeAlgorithm(2, DelayMilliseconds: 1_000),
            new ProbeAlgorithm(3, DelayMilliseconds: 1_000));
        var run = ExperimentTestSupport.CreateRun(experiment);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cancellation.CancelAfter(TimeSpan.FromMilliseconds(30));

        await Should.ThrowAsync<OperationCanceledException>(() =>
            run.CompleteAsync(ExecutionConcurrency.Concurrent(2), cancellationToken: cancellation.Token));
    }

    [Fact]
    public async Task DisposingConcurrentStream_CancelsActiveTrialsAndLeavesPendingTrialsUnstarted()
    {
        var probe = new ExecutionProbe();
        var algorithm = new ProbeAlgorithm(1, probe, HoldAfterYieldMilliseconds: 1_000);
        var run = algorithm.Repeat(4).CreateRun(MetaAlgorithmTestHelpers.CreateIntegerProblem(), RandomNumberGenerator.Create(42));

        await using (var enumerator = run.Stream(ExecutionConcurrency.Concurrent(2), cancellationToken: TestContext.Current.CancellationToken).GetAsyncEnumerator(TestContext.Current.CancellationToken))
        {
            (await enumerator.MoveNextAsync()).ShouldBeTrue();
        }

        probe.ExecutionCount.ShouldBe(2);
        probe.ActiveExecutions.ShouldBe(0);
    }

    [Fact]
    public async Task StartTrials_CancellationSettlesActiveAndPendingTasks()
    {
        var probe = new ExecutionProbe();
        var algorithm = new ProbeAlgorithm(1, probe, DelayMilliseconds: 1_000);
        var run = algorithm.Repeat(4).CreateRun(MetaAlgorithmTestHelpers.CreateIntegerProblem(), RandomNumberGenerator.Create(42));
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

        var trialTasks = run.StartTrials(ExecutionConcurrency.Concurrent(2), cancellationToken: cancellation.Token);
        while (probe.ExecutionCount < 2)
        {
            await Task.Delay(1, TestContext.Current.CancellationToken);
        }

        await cancellation.CancelAsync();
        await Should.ThrowAsync<OperationCanceledException>(() => Task.WhenAll(trialTasks));

        trialTasks.ShouldAllBe(task => task.IsCompleted);
        probe.ExecutionCount.ShouldBe(2);
        probe.ActiveExecutions.ShouldBe(0);
    }

    [Fact]
    public async Task ConcurrentWithOne_LimitsActiveTrialsWithoutChangingCategory()
    {
        var probe = new ExecutionProbe();
        var algorithm = new ProbeAlgorithm(1, probe, DelayMilliseconds: 10);
        var run = algorithm.Repeat(3).CreateRun(MetaAlgorithmTestHelpers.CreateIntegerProblem(), RandomNumberGenerator.Create(42));
        var concurrency = ExecutionConcurrency.Concurrent(1);

        _ = await run.CompleteAsync(concurrency, cancellationToken: TestContext.Current.CancellationToken);

        concurrency.Kind.ShouldBe(ExecutionConcurrencyKind.Concurrent);
        probe.ExecutionCount.ShouldBe(3);
        probe.MaximumActiveExecutions.ShouldBe(1);
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
            run.CompleteAsync(ExecutionConcurrency.Concurrent(2), cancellationToken: TestContext.Current.CancellationToken));

        exception.InnerExceptions.Cast<ExperimentTrialException<int>>().Select(failure => failure.Key).ShouldBe([0, 2]);
        probe.Candidates.ShouldBe([1]);
        probe.SetupCount.ShouldBe(3);
        probe.ExecutionCount.ShouldBe(2);
    }

    [Fact]
    public async Task Stream_AggregatesFailuresInMaterializationOrderAndContinuesUnaffectedTrials()
    {
        var probe = new ExecutionProbe();
        var experiment = CreateExperiment(
            new ProbeAlgorithm(0, probe, FailDuringExecution: true),
            new ProbeAlgorithm(1, probe),
            new ProbeAlgorithm(2, probe, FailDuringSetup: true));
        var run = ExperimentTestSupport.CreateRun(experiment);

        var exception = await Should.ThrowAsync<AggregateException>(async () =>
            _ = await run.Stream(ExecutionConcurrency.Concurrent(2), cancellationToken: TestContext.Current.CancellationToken)
                .ToListAsync(TestContext.Current.CancellationToken));

        exception.InnerExceptions.Cast<ExperimentTrialException<int>>().Select(failure => failure.Key).ShouldBe([0, 2]);
        probe.Candidates.ShouldBe([1]);
    }

    [Fact]
    public async Task StartTrials_ExposesSuccessfulAndFailedTrialTasks()
    {
        var probe = new ExecutionProbe();
        var experiment = CreateExperiment(
            new ProbeAlgorithm(0, probe, FailDuringExecution: true),
            new ProbeAlgorithm(1, probe),
            new ProbeAlgorithm(2, probe, FailDuringSetup: true));
        var run = ExperimentTestSupport.CreateRun(experiment);

        var trialTasks = run.StartTrials(ExecutionConcurrency.Concurrent(2), cancellationToken: TestContext.Current.CancellationToken);

        _ = await Should.ThrowAsync<ExperimentTrialException<int>>(() => Task.WhenAll(trialTasks));

        trialTasks.Select(task => task.Status).ShouldBe([TaskStatus.Faulted, TaskStatus.RanToCompletion, TaskStatus.Faulted]);
        var successfulResult = await trialTasks[1];
        successfulResult.Trial.Key.ShouldBe(1);
        MetaAlgorithmTestHelpers.StateCandidate(successfulResult.State).ShouldBe(1);
        var firstFailure = await Should.ThrowAsync<ExperimentTrialException<int>>(() => trialTasks[0]);
        var secondFailure = await Should.ThrowAsync<ExperimentTrialException<int>>(() => trialTasks[2]);
        new[] { firstFailure.Key, secondFailure.Key }.ShouldBe([0, 2]);
        probe.Candidates.ShouldBe([1]);
    }

    [Fact]
    public async Task StartTrials_SequentialExecutionStartsTrialsInMaterializationOrder()
    {
        var probe = new ExecutionProbe();
        var experiment = CreateExperiment(
            new ProbeAlgorithm(1, probe, DelayMilliseconds: 10),
            new ProbeAlgorithm(2, probe, DelayMilliseconds: 10),
            new ProbeAlgorithm(3, probe, DelayMilliseconds: 10));
        var run = ExperimentTestSupport.CreateRun(experiment);

        var trialTasks = run.StartTrials(ExecutionConcurrency.Sequential(), cancellationToken: TestContext.Current.CancellationToken);
        _ = await Task.WhenAll(trialTasks);

        probe.MaximumActiveExecutions.ShouldBe(1);
        probe.Candidates.ShouldBe([1, 2, 3]);
    }

    [Fact]
    public async Task Complete_ReportsTrialsThatYieldNoState()
    {
        var experiment = CreateExperiment(new ProbeAlgorithm(0, YieldState: false), new ProbeAlgorithm(1));
        var run = ExperimentTestSupport.CreateRun(experiment);

        var exception = await Should.ThrowAsync<AggregateException>(() =>
            run.CompleteAsync(ExecutionConcurrency.Sequential(), cancellationToken: TestContext.Current.CancellationToken));

        var trialException = exception.InnerExceptions.Single().ShouldBeOfType<ExperimentTrialException<int>>();
        trialException.Key.ShouldBe(0);
        trialException.InnerException.ShouldBeOfType<InvalidOperationException>();
    }

    [Fact]
    public async Task Stream_AllowsTrialsThatYieldNoState()
    {
        var experiment = CreateExperiment(new ProbeAlgorithm(0, YieldState: false), new ProbeAlgorithm(1));
        var run = ExperimentTestSupport.CreateRun(experiment);

        var entries = await run.Stream(ExecutionConcurrency.Sequential(), cancellationToken: TestContext.Current.CancellationToken)
            .ToListAsync(TestContext.Current.CancellationToken);

        entries.Select(entry => entry.Trial.Key).ShouldBe([1]);
        entries.Select(entry => MetaAlgorithmTestHelpers.StateCandidate(entry.State)).ShouldBe([1]);
    }

    [Fact]
    public async Task Complete_AggregatesExecutionAndMissingStateFailures()
    {
        var experiment = CreateExperiment(
            new ProbeAlgorithm(0, FailDuringExecution: true),
            new ProbeAlgorithm(1, YieldState: false),
            new ProbeAlgorithm(2));
        var run = ExperimentTestSupport.CreateRun(experiment);

        var exception = await Should.ThrowAsync<AggregateException>(() =>
            run.CompleteAsync(ExecutionConcurrency.Concurrent(), cancellationToken: TestContext.Current.CancellationToken));

        exception.InnerExceptions.Cast<ExperimentTrialException<int>>().Select(failure => failure.Key).ShouldBe([0, 1]);
    }

    private static FixedExperiment<ProbeAlgorithm> CreateExperiment(params IReadOnlyList<ProbeAlgorithm> algorithms) =>
        new([.. algorithms.Select((algorithm, index) => ExperimentCase.From(algorithm, index, [index]))]);
}
