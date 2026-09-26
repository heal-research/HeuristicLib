using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.ExecutionInfrastructure;

public class AlgorithmRunTests
{
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
    public void AnAnalyzerDecoratingDirectly_ObservesInTheOrderItWasAttached()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var algorithm = new AdditiveStepAlgorithm(1);
        var observed = new List<string>();

        _ = algorithm.CreateRun(problem, RandomNumberGenerator.Create(42))
                     .Attach(new ObservingAnalyzer(algorithm.Evaluator, () => observed.Add("first")))
                     .Attach(new DecoratingAnalyzer(algorithm.Evaluator, () => observed.Add("second")))
                     .Complete(cancellationToken: TestContext.Current.CancellationToken);

        // Both bind as modules, so the one attached first sits innermost and observes first. Recorded as
        // configuration, the direct decoration would sit inside every module whatever the attachment order.
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

    // Declares its decoration itself rather than through a module, as a hand-written analyzer may.
    private sealed class DecoratingAnalyzer(IEvaluator<int> evaluator, Action observed) : IAnalyzer
    {
        public void Install(ResolutionScopeBuilder builder) =>
            builder.Decorate(evaluator, current =>
                new ObservingEvaluator<int, ISearchSpace<int>, IProblem<int, ISearchSpace<int>>>(evaluator, current, _ => observed()));
    }

    private static int StateValue(PopulationState<int> state) =>
        state.Population.EvaluatedCandidates.Single().Candidate;

    private sealed record SequenceAlgorithm
        : Algorithm<SequenceAlgorithm, int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>
    {
        public override AlgorithmInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>
            CreateExecutionInstance(ResolutionScope scope) => new Instance();

        private sealed class Instance
            : AlgorithmInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>
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
