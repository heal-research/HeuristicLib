using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests.Experiments.TestSupport;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.ExecutionInfrastructure;

public class AlgorithmRunTests
{
    [Fact]
    public async Task Stream_ResolvesOnceBeforeEnumerationAndKeepsTheExecutorsOnResume()
    {
        var evaluator = new CountingResolutionEvaluator();
        var algorithm = new CountingExecutionAlgorithm(1, evaluator);
        var aspect = new CheckCountingAspect();
        var run = algorithm.CreateRun(MetaAlgorithmTestHelpers.CreateIntegerProblem(), RandomNumberGenerator.Create(42))
            .AddAspect(aspect);
        var cancellationToken = TestContext.Current.CancellationToken;

        algorithm.ExecutionCount.ShouldBe(0);
        aspect.CheckCount.ShouldBe(0);
        var stream = run.Stream(cancellationToken: cancellationToken);

        run.LifecycleState.ShouldBe(RunLifecycleState.Running);
        algorithm.ExecutionCount.ShouldBe(1);
        evaluator.ExecutionCount.ShouldBe(1);
        aspect.CheckCount.ShouldBe(1);
        await using (var enumerator = stream.GetAsyncEnumerator(cancellationToken))
            (await enumerator.MoveNextAsync()).ShouldBeTrue();

        run.LifecycleState.ShouldBe(RunLifecycleState.Paused);
        Should.Throw<InvalidOperationException>(() => run.AddAspect(new BlindAnalyzer()));
        _ = await run.Stream(cancellationToken: cancellationToken).ToListAsync(cancellationToken);

        run.LifecycleState.ShouldBe(RunLifecycleState.Completed);
        algorithm.ExecutionCount.ShouldBe(1);
        evaluator.ExecutionCount.ShouldBe(1);
        aspect.CheckCount.ShouldBe(1);
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
        Should.Throw<InvalidOperationException>(() => run.AddAspect(new BlindAnalyzer()));
        run.LifecycleState.ShouldBe(RunLifecycleState.Failed);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AdviceFailure_IsTerminal(bool canceled)
    {
        Exception failure = canceled ? new OperationCanceledException() : new InvalidOperationException();
        var run = new SequenceAlgorithm().CreateRun(MetaAlgorithmTestHelpers.CreateIntegerProblem(), RandomNumberGenerator.Create(42))
            .AddAspect(new FailingModule(failure));
        var expectedState = canceled ? RunLifecycleState.Canceled : RunLifecycleState.Failed;

        var thrown = Record.Exception(() => run.Stream(cancellationToken: TestContext.Current.CancellationToken));

        thrown.ShouldBeSameAs(failure);
        run.LifecycleState.ShouldBe(expectedState);
        Should.Throw<InvalidOperationException>(() => run.Stream(cancellationToken: TestContext.Current.CancellationToken));
        Should.Throw<InvalidOperationException>(() => run.AddAspect(new BlindAnalyzer()));
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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CompetingStream_IsRejectedWithoutFailingTheActiveExecution(bool startEnumeration)
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var run = new SequenceAlgorithm().CreateRun(problem, RandomNumberGenerator.Create(42));
        var cancellationToken = TestContext.Current.CancellationToken;
        var stream = run.Stream(cancellationToken: cancellationToken);
        await using var enumerator = stream.GetAsyncEnumerator(cancellationToken);
        var values = new List<int>();
        if (startEnumeration)
        {
            (await enumerator.MoveNextAsync()).ShouldBeTrue();
            values.Add(StateValue(enumerator.Current));
        }

        Should.Throw<InvalidOperationException>(() => run.Stream(cancellationToken: cancellationToken))
              .Message.ShouldContain("active execution stream");
        run.LifecycleState.ShouldBe(RunLifecycleState.Running);

        while (await enumerator.MoveNextAsync())
            values.Add(StateValue(enumerator.Current));

        values.ShouldBe([1, 2, 3]);
        run.LifecycleState.ShouldBe(RunLifecycleState.Completed);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CompletionDisposalFailure_IsTerminalAndPreservesTheException(bool canceled)
    {
        Exception failure = canceled ? new OperationCanceledException() : new InvalidOperationException("Disposal failed.");
        var disposalCount = 0;
        var algorithm = new DisposalTrackingAlgorithm(() =>
        {
            disposalCount++;
            return ValueTask.FromException(failure);
        });
        var run = algorithm.CreateRun(MetaAlgorithmTestHelpers.CreateIntegerProblem(), RandomNumberGenerator.Create(42));
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var enumerator = run.Stream(cancellationToken: cancellationToken).GetAsyncEnumerator(cancellationToken);
        (await enumerator.MoveNextAsync()).ShouldBeTrue();
        StateValue(enumerator.Current).ShouldBe(1);

        var thrown = await Record.ExceptionAsync(async () => _ = await enumerator.MoveNextAsync());

        thrown.ShouldBeSameAs(failure);
        disposalCount.ShouldBe(1);
        run.LifecycleState.ShouldBe(RunLifecycleState.Failed);
        Should.Throw<InvalidOperationException>(() => run.Stream(cancellationToken: cancellationToken));
        Should.Throw<InvalidOperationException>(() => run.AddAspect(new BlindAnalyzer()));
        await run.CancelAsync();
        disposalCount.ShouldBe(1);
        run.LifecycleState.ShouldBe(RunLifecycleState.Failed);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task TerminalCancellation_DisposesOnceAndRemainsCanceled(bool paused, bool failDuringDisposal)
    {
        var failure = new InvalidOperationException("Disposal failed.");
        var disposalCount = 0;
        var algorithm = new DisposalTrackingAlgorithm(() =>
        {
            disposalCount++;
            return failDuringDisposal ? ValueTask.FromException(failure) : ValueTask.CompletedTask;
        });
        var run = algorithm.CreateRun(MetaAlgorithmTestHelpers.CreateIntegerProblem(), RandomNumberGenerator.Create(42));
        var cancellationToken = TestContext.Current.CancellationToken;
        var stream = paused ? run.Stream(cancellationToken: cancellationToken) : run.StreamForTerminalCancellation(null, cancellationToken);
        await using (var enumerator = stream.GetAsyncEnumerator(cancellationToken))
            (await enumerator.MoveNextAsync()).ShouldBeTrue();
        run.LifecycleState.ShouldBe(paused ? RunLifecycleState.Paused : RunLifecycleState.Running);
        disposalCount.ShouldBe(0);

        var thrown = await Record.ExceptionAsync(async () => await run.CancelAsync());

        if (failDuringDisposal)
            thrown.ShouldBeSameAs(failure);
        else
            thrown.ShouldBeNull();
        disposalCount.ShouldBe(1);
        run.LifecycleState.ShouldBe(RunLifecycleState.Canceled);
        Should.Throw<InvalidOperationException>(() => run.Stream(cancellationToken: cancellationToken));
        Should.Throw<InvalidOperationException>(() => run.AddAspect(new BlindAnalyzer()));
        await run.CancelAsync();
        disposalCount.ShouldBe(1);
        run.LifecycleState.ShouldBe(RunLifecycleState.Canceled);
    }

    [Fact]
    public void AlgorithmRun_RejectsAttachmentsAfterExecutionStarts()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var run = new AdditiveStepAlgorithm(1).CreateRun(problem, RandomNumberGenerator.Create(42));

        _ = run.Stream(cancellationToken: TestContext.Current.CancellationToken);

        var exception = Should.Throw<InvalidOperationException>(() => run.AddAspect(new BlindAnalyzer()));
        exception.Message.ShouldContain("its current lifecycle state is Running");
    }

    [Fact]
    public void AnAnalyzerWrappingDirectly_ObservesInTheOrderItWasAttached()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var algorithm = new AdditiveStepAlgorithm(1);
        var observed = new List<string>();

        _ = algorithm.CreateRun(problem, RandomNumberGenerator.Create(42))
                     .AddAspect(new ObservingAnalyzer(algorithm.Evaluator, () => observed.Add("first")))
                     .AddAspect(new WrappingAnalyzer(algorithm.Evaluator, () => observed.Add("second")))
                     .Complete(cancellationToken: TestContext.Current.CancellationToken);

        // Both bind as modules, so the one attached first sits innermost and observes first. Recorded as
        // configuration, the direct wrapper would sit inside every module whatever the attachment order.
        observed.ShouldBe(["first", "second"]);
    }

    [Fact]
    public void TheSameAspectAddedTwice_ChecksItsAdviceOnce()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var run = new AdditiveStepAlgorithm(1).CreateRun(problem, RandomNumberGenerator.Create(42));
        var aspect = new CheckCountingAspect();

        run.AddAspect(aspect).AddAspect(aspect);

        run.Complete(cancellationToken: TestContext.Current.CancellationToken);

        aspect.CheckCount.ShouldBe(1);
    }

    private sealed class FailingModule : Aspect
    {
        public FailingModule(Exception failure)
        {
            Advices = [(Pointcut.OfType<IAlgorithm<int, PopulationState<int>>>(), new ThrowingAlgorithmAdvice(failure))];
        }
    }

    private sealed class ThrowingAlgorithmAdvice(Exception failure) : AlgorithmAdvice<int, PopulationState<int>>
    {
        public override IAlgorithmExecutor<int, TSearchSpace, TProblem, PopulationState<int>> Wrap<TSearchSpace, TProblem>(
            IAlgorithmExecutor<int, TSearchSpace, TProblem, PopulationState<int>> executor, IAlgorithm<int, PopulationState<int>> source) =>
            throw failure;
    }

    private sealed class CheckingEvaluatorAdvice(Action check) : OperatorAdvice<Evaluation, int>
    {
        public override void Check<TSearchSpace, TProblem, TSearchState>(OperationInfo operation,
            IOperator<Evaluation, int> source) => check();

        public override IExecutor Wrap<TSearchSpace, TProblem, TSearchState>(IExecutor executor, OperationInfo operation,
            IOperator<Evaluation, int> source) => executor;
    }

    private sealed class CheckCountingAspect : Aspect
    {
        public int CheckCount { get; private set; }
        public CheckCountingAspect()
        {
            Advices = [(Pointcut.OfType<IOperator<Evaluation, int>>(), new CheckingEvaluatorAdvice(() => CheckCount++))];
        }
    }

    private sealed class BlindAnalyzer : Aspect;

    private sealed class ObservingAnalyzer : Aspect
    {
        public ObservingAnalyzer(IOperator<Evaluation, int> evaluator, Action observed)
        {
            Advices = [.. Aspect.Observe(evaluator.Calls, _ => observed())];
        }
    }

    private sealed class WrappingAnalyzer : Aspect
    {
        public WrappingAnalyzer(IOperator<Evaluation, int> evaluator, Action observed)
        {
            Advices = [(Pointcut.Reference(evaluator), new ObservingEvaluatorAdvice(observed))];
        }
    }

    private sealed class ObservingEvaluatorAdvice(Action observed) : EvaluatorAdvice<int>
    {
        protected override IEvaluationExecutor<int, TSearchSpace, TProblem> Wrap<TSearchSpace, TProblem>(
            IEvaluationExecutor<int, TSearchSpace, TProblem> executor, IOperator<Evaluation, int> evaluator) =>
            new Executor<TSearchSpace, TProblem>(executor, observed);

        private sealed class Executor<TSearchSpace, TProblem>(IEvaluationExecutor<int, TSearchSpace, TProblem> executor, Action observed)
            : IEvaluationExecutor<int, TSearchSpace, TProblem>
            where TSearchSpace : class, ISearchSpace<int>
            where TProblem : class, IProblem<int, TSearchSpace>
        {
            public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<int> candidates, IRandomNumberGenerator random,
                TSearchSpace searchSpace, TProblem problem)
            {
                var objectiveVectors = executor.Evaluate(candidates, random, searchSpace, problem);
                observed();
                return objectiveVectors;
            }
        }
    }

    private static int StateValue(PopulationState<int> state) =>
        state.Population.EvaluatedCandidates.Single().Candidate;

    private sealed record DisposalTrackingAlgorithm(Func<ValueTask> Dispose)
        : Algorithm<DisposalTrackingAlgorithm, int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>
    {
        protected override IAlgorithmExecutor<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>> CreateExecutor(
            ResolutionScope<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>> scope) =>
            new Executor(Dispose);

        private sealed class Executor(Func<ValueTask> dispose)
            : AlgorithmExecutor<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>,
              IAsyncEnumerable<PopulationState<int>>, IAsyncEnumerator<PopulationState<int>>
        {
            private bool yielded;

            public PopulationState<int> Current => Population.From([1.ToEvaluated(1)]).ToPopulationState();

            public override IAsyncEnumerable<PopulationState<int>> RunStreamingAsync(
                IProblem<int, DummySearchSpace<int>> problem,
                IRandomNumberGenerator random,
                PopulationState<int>? initialState = null,
                CancellationToken ct = default) => this;

            public IAsyncEnumerator<PopulationState<int>> GetAsyncEnumerator(CancellationToken cancellationToken = default) => this;

            public ValueTask<bool> MoveNextAsync()
            {
                var hasNext = !yielded;
                yielded = true;
                return ValueTask.FromResult(hasNext);
            }

            public ValueTask DisposeAsync() => dispose();
        }
    }

    private sealed record SequenceAlgorithm
        : Algorithm<SequenceAlgorithm, int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>
    {
        protected override IAlgorithmExecutor<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>> CreateExecutor(
            ResolutionScope<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>> scope) =>
            new Executor();

        private sealed class Executor
            : AlgorithmExecutor<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>
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
