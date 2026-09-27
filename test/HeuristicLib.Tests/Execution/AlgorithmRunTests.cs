using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests.Experiments.TestSupport;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.ExecutionInfrastructure;

public class AlgorithmRunTests
{
    [Fact]
    public async Task Stream_PreparesOnceBeforeEnumerationAndReusesPreparationOnResume()
    {
        var evaluator = new CountingResolutionEvaluator();
        var algorithm = new CountingExecutionAlgorithm(1, evaluator);
        var module = new DualRoleAttachment();
        var run = algorithm.CreateRun(MetaAlgorithmTestHelpers.CreateIntegerProblem(), RandomNumberGenerator.Create(42))
            .Attach(module);
        var cancellationToken = TestContext.Current.CancellationToken;

        algorithm.ExecutionCount.ShouldBe(0);
        module.InstallationCount.ShouldBe(0);
        var stream = run.Stream(cancellationToken: cancellationToken);

        run.LifecycleState.ShouldBe(RunLifecycleState.Running);
        algorithm.ExecutionCount.ShouldBe(1);
        evaluator.ExecutionCount.ShouldBe(1);
        module.InstallationCount.ShouldBe(1);
        await using (var enumerator = stream.GetAsyncEnumerator(cancellationToken))
            (await enumerator.MoveNextAsync()).ShouldBeTrue();

        run.LifecycleState.ShouldBe(RunLifecycleState.Paused);
        Should.Throw<InvalidOperationException>(() => run.Attach(new BlindAnalyzer()));
        _ = await run.Stream(cancellationToken: cancellationToken).ToListAsync(cancellationToken);

        run.LifecycleState.ShouldBe(RunLifecycleState.Completed);
        algorithm.ExecutionCount.ShouldBe(1);
        evaluator.ExecutionCount.ShouldBe(1);
        module.InstallationCount.ShouldBe(1);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Cancellation_PausesAndAllowsResume(bool cancelStreamToken)
    {
        var run = new SequenceAlgorithm().CreateRun(MetaAlgorithmTestHelpers.CreateIntegerProblem(), RandomNumberGenerator.Create(42));
        var testToken = TestContext.Current.CancellationToken;
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(testToken);
        var stream = run.Stream(cancellationToken: cancelStreamToken ? cancellation.Token : testToken);

        await using (var enumerator = stream.GetAsyncEnumerator(cancelStreamToken ? testToken : cancellation.Token))
        {
            (await enumerator.MoveNextAsync()).ShouldBeTrue();
            StateValue(enumerator.Current).ShouldBe(1);
            await cancellation.CancelAsync();
            await Should.ThrowAsync<OperationCanceledException>(async () => await enumerator.MoveNextAsync());
            run.LifecycleState.ShouldBe(RunLifecycleState.Paused);
        }

        var remaining = await run.Stream(cancellationToken: testToken).ToListAsync(testToken);

        remaining.Select(StateValue).ShouldBe([2, 3]);
        run.LifecycleState.ShouldBe(RunLifecycleState.Completed);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AlgorithmFailure_IsTerminal(bool failDuringSetup)
    {
        var algorithm = new ProbeAlgorithm(1, FailDuringSetup: failDuringSetup, FailDuringExecution: !failDuringSetup);
        var run = algorithm.CreateRun(MetaAlgorithmTestHelpers.CreateIntegerProblem(), RandomNumberGenerator.Create(42));
        var cancellationToken = TestContext.Current.CancellationToken;

        await Should.ThrowAsync<InvalidOperationException>(async () =>
            _ = await run.Stream(cancellationToken: cancellationToken).ToListAsync(cancellationToken));

        run.LifecycleState.ShouldBe(RunLifecycleState.Failed);
        Should.Throw<InvalidOperationException>(() => run.Stream(cancellationToken: cancellationToken));
        Should.Throw<InvalidOperationException>(() => run.Attach(new BlindAnalyzer()));
        run.LifecycleState.ShouldBe(RunLifecycleState.Failed);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void InstallationFailure_IsTerminal(bool canceled)
    {
        Exception failure = canceled ? new OperationCanceledException() : new InvalidOperationException();
        var run = new SequenceAlgorithm().CreateRun(MetaAlgorithmTestHelpers.CreateIntegerProblem(), RandomNumberGenerator.Create(42))
            .Attach(new FailingModule(failure));
        var expectedState = canceled ? RunLifecycleState.Canceled : RunLifecycleState.Failed;

        var thrown = Record.Exception(() => run.Stream(cancellationToken: TestContext.Current.CancellationToken));

        thrown.ShouldBeSameAs(failure);
        run.LifecycleState.ShouldBe(expectedState);
        Should.Throw<InvalidOperationException>(() => run.Stream(cancellationToken: TestContext.Current.CancellationToken));
        Should.Throw<InvalidOperationException>(() => run.Attach(new BlindAnalyzer()));
        run.LifecycleState.ShouldBe(expectedState);
    }

    [Fact]
    public void CompletedRun_ReturnsAnEmptyStream()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var run = new AdditiveStepAlgorithm(1).CreateRun(problem, RandomNumberGenerator.Create(42));

        run.LifecycleState.ShouldBe(RunLifecycleState.Preparing);
        _ = run.Complete(cancellationToken: TestContext.Current.CancellationToken);
        run.LifecycleState.ShouldBe(RunLifecycleState.Completed);

        run.Stream(cancellationToken: TestContext.Current.CancellationToken).ShouldBeEmpty();
    }

    [Fact]
    public async Task PausedRun_ContinuesTheSameExecution()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var run = new SequenceAlgorithm().CreateRun(problem, RandomNumberGenerator.Create(42));

        await using (var first = run.Stream().GetAsyncEnumerator())
        {
            (await first.MoveNextAsync()).ShouldBeTrue();
            first.Current.Population.EvaluatedCandidates.Single().Candidate.ShouldBe(1);
        }

        run.LifecycleState.ShouldBe(RunLifecycleState.Paused);

        var remaining = await run.Stream().ToListAsync();

        remaining.Select(StateValue).ShouldBe([2, 3]);
        run.LifecycleState.ShouldBe(RunLifecycleState.Completed);
    }

    [Fact]
    public void AlgorithmRun_AllowsOnlyOneActiveStream()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var run = new SequenceAlgorithm().CreateRun(problem, RandomNumberGenerator.Create(42));

        _ = run.Stream();

        Should.Throw<InvalidOperationException>(() => run.Stream())
              .Message.ShouldContain("active execution stream");
    }

    [Fact]
    public void AlgorithmRun_RejectsAttachmentsAfterExecutionStarts()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var run = new AdditiveStepAlgorithm(1).CreateRun(problem, RandomNumberGenerator.Create(42));

        _ = run.Stream(cancellationToken: TestContext.Current.CancellationToken);

        var exception = Should.Throw<InvalidOperationException>(() => run.Attach(new BlindAnalyzer()));
        exception.Message.ShouldContain("its current lifecycle state is Running");
    }

    [Fact]
    public void AnAnalyzerWrappingDirectly_ObservesInTheOrderItWasAttached()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var algorithm = new AdditiveStepAlgorithm(1);
        var observed = new List<string>();

        _ = algorithm.CreateRun(problem, RandomNumberGenerator.Create(42))
                     .Attach(new ObservingAnalyzer(algorithm.Evaluator, () => observed.Add("first")))
                     .Attach(new WrappingAnalyzer(algorithm.Evaluator, () => observed.Add("second")))
                     .Complete(cancellationToken: TestContext.Current.CancellationToken);

        // Both bind as modules, so the one attached first sits innermost and observes first. Recorded as
        // configuration, the direct wrapper would sit inside every module whatever the attachment order.
        observed.ShouldBe(["first", "second"]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TheSameAttachmentThroughBothInterfaces_IsInstalledOnce(bool analyzerFirst)
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var run = new AdditiveStepAlgorithm(1).CreateRun(problem, RandomNumberGenerator.Create(42));
        var attachment = new DualRoleAttachment();

        if (analyzerFirst)
            run.Attach((IAnalyzer)attachment).Attach((IExecutionModule)attachment);
        else
            run.Attach((IExecutionModule)attachment).Attach((IAnalyzer)attachment);

        run.Complete(cancellationToken: TestContext.Current.CancellationToken);

        attachment.InstallationCount.ShouldBe(1);
    }

    private sealed class FailingModule(Exception failure) : IExecutionModule
    {
        public void Install(ResolutionScopeBuilder builder) => throw failure;
    }

    private sealed class DualRoleAttachment : IAnalyzer
    {
        public int InstallationCount { get; private set; }

        public void Install(ResolutionScopeBuilder builder) => InstallationCount++;
    }

    private sealed class BlindAnalyzer : IAnalyzer
    {
        public void Install(ResolutionScopeBuilder builder)
        {
        }
    }

    private sealed class ObservingAnalyzer(IEvaluator<int> evaluator, Action observed) : IAnalyzer
    {
        public void Install(ResolutionScopeBuilder builder) => builder.Observe(evaluator, _ => observed());
    }

    // Declares its wrapper itself rather than through a module, as a hand-written analyzer may.
    private sealed class WrappingAnalyzer(IEvaluator<int> evaluator, Action observed) : IAnalyzer
    {
        public void Install(ResolutionScopeBuilder builder) =>
            builder.Wrap(evaluator, current =>
                new ObservingEvaluator<int, ISearchSpace<int>, IProblem<int, ISearchSpace<int>>>(evaluator, current, _ => observed()));
    }

    private static int StateValue(PopulationState<int> state) =>
        state.Population.EvaluatedCandidates.Single().Candidate;

    private sealed record SequenceAlgorithm
        : Algorithm<SequenceAlgorithm, int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>
    {
        public override AlgorithmExecution<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>
            CreateExecutionInstance(ResolutionScope scope) => new Execution();

        private sealed class Execution
            : AlgorithmExecution<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>
        {
            public override async IAsyncEnumerable<PopulationState<int>> RunStreamingAsync(
                IProblem<int, DummySearchSpace<int>> problem,
                IRandomNumberGenerator random,
                PopulationState<int>? initialState = null,
                [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
            {
                foreach (var value in Enumerable.Range(1, 3))
                {
                    ct.ThrowIfCancellationRequested();
                    yield return Population.From([value.ToEvaluated(value)]).ToPopulationState();
                    await Task.Yield();
                }
            }
        }
    }
}
