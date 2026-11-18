using HEAL.HeuristicLib.Encodings.Permutations;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Refiners;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Analysis;

public sealed class SimpleAnalysisTests
{
    private static readonly IProblem<int, DummySearchSpace<int>> Problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();

    [Fact]
    public void CounterClocksOfOneNodeAreEqualAndReadTheSameTraceAxis()
    {
        var algorithm = new AdditiveStepAlgorithm(1);
        var counter = new Counter();
        var first = counter.Clock;
        var second = counter.Clock;
        first.ShouldBe(second);
        first.ShouldNotBeSameAs(second);
        var trace = algorithm.Evaluator.Invocations.Trace(_ => 1, [first, second]);
        var run = algorithm.CreateRun(Problem, RandomNumberGenerator.Create(1), [trace, algorithm.Evaluator.Invocations.Counting(counter)]);

        run.Complete(cancellationToken: TestContext.Current.CancellationToken);

        run.Result(trace).Single().At(counter.Clock).ShouldBe(1);
        run.Result(trace).By(counter.Clock).Select(point => point.Time).ShouldBe([1L]);
        run.Result<IClockReading<long>>(first).ShouldBeSameAs(run.Result<IClockReading<long>>(first));
        run.Result<IClockReading<long>>(second).ShouldNotBeSameAs(run.Result<IClockReading<long>>(first));
    }

    [Fact]
    public void ClocksOfDifferentCountersIdentifySeparateColumns()
    {
        var algorithm = new AdditiveStepAlgorithm(1);
        var counted = new Counter();
        var unplaced = new Counter();
        counted.Clock.ShouldNotBe(unplaced.Clock);
        var trace = algorithm.Evaluator.Invocations.Trace(_ => 1, [counted.Clock, unplaced.Clock]);
        var run = algorithm.CreateRun(Problem, RandomNumberGenerator.Create(1), [trace, algorithm.Evaluator.Invocations.Counting(counted)]);

        run.Complete(cancellationToken: TestContext.Current.CancellationToken);

        var entry = run.Result(trace).Single();
        entry.At(counted.Clock).ShouldBe(1);
        entry.At(unplaced.Clock).ShouldBe(0);
    }

    [Fact]
    public void AnEqualElapsedClockReadsTraceAndBestSoFarColumns()
    {
        var algorithm = new AdditiveStepAlgorithm(1);
        var trace = algorithm.Evaluator.Invocations.Trace(_ => 1, [Clock.Elapsed]);
        var best = Analyzer.BestSoFar(algorithm.Evaluator, [Clock.Elapsed]);
        var run = algorithm.CreateRun(Problem, RandomNumberGenerator.Create(1), [trace, best]);

        run.Complete(cancellationToken: TestContext.Current.CancellationToken);

        var equal = new ElapsedClock();
        equal.ShouldBe(Clock.Elapsed);
        var result = run.Result(trace);
        result.Single().At(equal).ShouldBe(result.Single().At(Clock.Elapsed));
        result.Snapshot().By(equal).ShouldBe(result.By(Clock.Elapsed));
        run.Result(best).Single().At(equal).ShouldBe(run.Result(best).Single().At(Clock.Elapsed));
    }

    [Fact]
    public void DurationClockReadsTheRootAccumulatorThroughANarrowerShare()
    {
        var duration = new Duration();
        duration.Clock.ShouldBe(duration.Clock);
        var root = ResolutionScope.Create();
        var child = root.CreateChildScope();
        child.Share(duration);
        var rootAccumulator = root.Results.Result(duration);
        var localAccumulator = child.Resolve(duration);
        var reading = child.Resolve<IClockReading<TimeSpan>>(duration.Clock);
        rootAccumulator.AddDuration(TimeSpan.FromSeconds(3));
        localAccumulator.AddDuration(TimeSpan.FromSeconds(7));

        reading.Read().ShouldBe(TimeSpan.FromSeconds(3));
        child.Results.Result<IClockReading<TimeSpan>>(duration.Clock).Read().ShouldBe(TimeSpan.FromSeconds(3));
        localAccumulator.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(7));
        rootAccumulator.AddDuration(TimeSpan.FromSeconds(2));
        reading.Read().ShouldBe(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void NestedGroupsExpandInPlaceAndDeduplicateAtTheLastPosition()
    {
        var algorithm = new AdditiveStepAlgorithm(1);
        var log = new List<string>();
        var first = algorithm.Evaluator.Invocations.Observing(_ => log.Add("first"));
        var second = algorithm.Evaluator.Invocations.Observing(_ => log.Add("second"));
        var group = new Group([first, new Group([second, first])]);
        group.Expand().ShouldBeSameAs(group.Expand());
        group.Expand()[0].ShouldBeSameAs(first.Expand()[0]);
        group.Expand()[2].ShouldBeSameAs(first.Expand()[0]);
        first.Expand()[0].Expand().ShouldBeSameAs(first.Expand()[0].Expand());
        algorithm.CreateRun(Problem, RandomNumberGenerator.Create(1), [group]).Complete(cancellationToken: TestContext.Current.CancellationToken);
        log.ShouldBe(["first", "second"]);
    }

    [Fact]
    public void RecursiveGroupsFailAndConcurrentReadsShareOneExpansion()
    {
        var recursive = new Group([]);
        recursive.Parts = [new Group([recursive])];
        Should.Throw<InvalidOperationException>(() => recursive.Expand()).Message.ShouldContain(nameof(Group));
        var group = new Group([Substitution.Of<Counter>(counter => counter)]);
        var reads = new IReadOnlyList<Substitution>[32];
        Parallel.For(0, reads.Length, i => reads[i] = group.Expand());
        reads.ShouldAllBe(read => ReferenceEquals(read, reads[0]));
    }

    [Fact]
    public void CreateRunPlacesSharedClocksBeforeBothTraces()
    {
        var algorithm = new AdditiveStepAlgorithm(1);
        var count = new Counter();
        var first = algorithm.Evaluator.Invocations.Trace(_ => 1, [count.Clock]);
        var second = algorithm.Evaluator.Invocations.Trace(_ => 2, [count.Clock]);
        var run = algorithm.CreateRun(Problem, RandomNumberGenerator.Create(1), [first, second, algorithm.Evaluator.Invocations.CountingCandidates(count)]);
        run.Complete(cancellationToken: TestContext.Current.CancellationToken);
        run.Result(count).CurrentCount.ShouldBe(1);
        run.Result(first).Single().At(count.Clock).ShouldBe(1);
        run.Result(second).Single().At(count.Clock).ShouldBe(1);
        var entries = new List<TraceEntry<int>>();
        foreach (var entry in run.Result(first))
            entries.Add(entry);
        entries.ShouldBe(run.Result(first).Snapshot());
    }

    [Fact]
    public void CreateRunRejectsUnheldAnalyzersAndUsesTheSubstitutedGraphForValidation()
    {
        var algorithm = new AdditiveStepAlgorithm(1);
        Should.Throw<InvalidOperationException>(() => algorithm.CreateRun(Problem, RandomNumberGenerator.Create(1),
            [Analyzer.BestSoFar<Permutation>()])).Message.ShouldContain("CreateRun(problem, random, [substitution])");
        var invalid = Substitution.Of(algorithm.Evaluator, _ => new UnsupportedEvaluator());
        Should.Throw<InvalidOperationException>(() => algorithm.CreateRun(Problem, RandomNumberGenerator.Create(1), [invalid]));
        algorithm.CreateRun(Problem, RandomNumberGenerator.Create(1), [invalid], validate: false).ShouldNotBeNull();
        algorithm.CreateRun(Problem, RandomNumberGenerator.Create(1)).ShouldNotBeNull();
    }

    [Fact]
    public void BudgetTargetsComeFromTheBudgetedConfiguration()
    {
        var algorithm = new AdditiveStepAlgorithm(1);
        var trace = algorithm.Iterations.Trace(_ => 1);
        var limited = algorithm.LimitedToInvocations(algorithm.Evaluator, 2);
        var message = Should.Throw<InvalidOperationException>(() =>
            limited.CreateRun(Problem, RandomNumberGenerator.Create(1), [trace])).Message;
        message.ShouldContain("limited.Algorithm.Iterations");
        message.ShouldContain("configuration it receives");
        var current = limited.Algorithm.Iterations.Trace(_ => 1);
        var run = limited.CreateRun(Problem, RandomNumberGenerator.Create(1), [current]);
        run.Complete(cancellationToken: TestContext.Current.CancellationToken);
        run.Result(current).Single().Value.ShouldBe(1);
    }

    [Fact]
    public void CreateRunAnalyzers_DoNotBecomePartOfTheOriginalConfiguration()
    {
        var algorithm = new AdditiveStepAlgorithm(1);
        var children = algorithm.GetParameters().SelectMany(parameter => parameter.GetChildren(algorithm)).ToArray();
        var best = Analyzer.BestSoFar<int>();
        var run = algorithm.CreateRun(Problem, RandomNumberGenerator.Create(1), [best]);

        run.Complete(cancellationToken: TestContext.Current.CancellationToken);

        var remaining = algorithm.GetParameters().SelectMany(parameter => parameter.GetChildren(algorithm)).ToArray();
        remaining.Length.ShouldBe(children.Length);
        for (var i = 0; i < children.Length; i++)
            remaining[i].ShouldBeSameAs(children[i]);
        remaining.ShouldNotContain(child => ReferenceEquals(child, best));
        run.Result(best).Single().Evaluations.ShouldBe(1);
        var plain = algorithm.CreateRun(Problem, RandomNumberGenerator.Create(1));
        plain.Complete(cancellationToken: TestContext.Current.CancellationToken);
        Should.Throw<InvalidOperationException>(() => plain.Result(best)).Message.ShouldContain("does not hold");
    }

    [Fact]
    public void CachedEvaluatorCountsOnlyMissesAndIncludesItsOwnCall()
    {
        IOperator<Evaluation, int> evaluator = new ProblemEvaluator<int>();
        var algorithm = new AdditiveStepAlgorithm(0) { Evaluator = evaluator.Cached() };
        var count = new Counter();
        var calls = new Counter();
        var best = Analyzer.BestSoFar<int>(clocks: [count.Clock, Clock.Elapsed], retention: TraceRetention.EveryObservation());
        var cycle = (CycleAlgorithm.Create(algorithm) with { MaximumCycles = 3 }).Sharing(algorithm.Evaluator);
        var run = cycle.CreateRun(Problem, RandomNumberGenerator.Create(1), [best, algorithm.Evaluator.Invocations.Counting(calls), evaluator.Invocations.CountingCandidates(count)]);
        run.Complete(cancellationToken: TestContext.Current.CancellationToken);
        var point = run.Result(best).Single();
        run.Result(calls).CurrentCount.ShouldBe(3);
        point.Candidate.ShouldBe(0);
        point.Value.ShouldBe(new ObjectiveVector(0));
        point.Evaluations.ShouldBe(1);
        point.At(count.Clock).ShouldBe(point.Evaluations);
        point.At(Clock.Elapsed).ShouldBeGreaterThan(TimeSpan.Zero);
        Should.Throw<InvalidOperationException>(() => point.At(new Counter().Clock)).Message.ShouldBe("The trace does not use the requested clock.");
    }

    [Fact]
    public void DefaultSelection_ObservesAnAlreadyCountedEvaluatorOnce()
    {
        IOperator<Evaluation, int> evaluator = new ProblemEvaluator<int>();
        var existing = new Counter();
        var best = Analyzer.BestSoFar<int>(retention: TraceRetention.EveryObservation());
        var scope = ResolutionScope.Create();
        var executor = Resolve(scope, evaluator.Counted(existing, OperatorCountMetric.Candidates).Substitute(best));

        executor.Evaluate([8, 6], RandomNumberGenerator.Create(1));
        scope.Results.Result(existing).CurrentCount.ShouldBe(2);
        scope.Results.Result(best.Evaluations).CurrentCount.ShouldBe(2);
        executor.Evaluate([5, 3, 4], RandomNumberGenerator.Create(1));

        var points = scope.Results.Result(best).ToArray();
        points.Length.ShouldBe(2);
        points.Select(point => point.Evaluations).ShouldBe([2L, 5L]);
        points.Select(point => point.Candidate).ShouldBe([6, 3]);
        scope.Results.Result(existing).CurrentCount.ShouldBe(5);
        scope.Results.Result(best.Evaluations).CurrentCount.ShouldBe(5);
    }

    [Fact]
    public void NamedWrappedEvaluator_OverridesDefaultSelection()
    {
        IOperator<Evaluation, int> evaluator = new ProblemEvaluator<int>();
        IOperator<Evaluation, int> cached = evaluator.Cached();
        var outer = Analyzer.BestSoFar(cached, retention: TraceRetention.EveryObservation());
        var misses = Analyzer.BestSoFar<int>(retention: TraceRetention.EveryObservation());
        var scope = ResolutionScope.Create();
        var executor = Resolve(scope, cached.Substitute(outer, misses));

        for (var i = 0; i < 3; i++)
            executor.Evaluate([4, 2], RandomNumberGenerator.Create(1));

        scope.Results.Result(outer).Select(point => point.Evaluations).ShouldBe([2L, 4L, 6L]);
        scope.Results.Result(outer).Select(point => point.Candidate).ShouldBe([2, 2, 2]);
        scope.Results.Result(outer).Select(point => point.Value).ShouldBe([new ObjectiveVector(2), new ObjectiveVector(2), new ObjectiveVector(2)]);
        var miss = scope.Results.Result(misses).Single();
        miss.Evaluations.ShouldBe(2);
        miss.Candidate.ShouldBe(2);
        miss.Value.ShouldBe(new ObjectiveVector(2));
        scope.Results.Result(outer.Evaluations).CurrentCount.ShouldBe(6);
        scope.Results.Result(misses.Evaluations).CurrentCount.ShouldBe(2);
    }

    private static IEvaluationExecutor<int> Resolve(ResolutionScope scope, IOperator<Evaluation, int> evaluator) =>
        scope.For<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(Problem).Resolve(evaluator);

    [Fact]
    public void DefaultSelection_IncludesEvaluatorsInsideRefiners()
    {
        var refiner = new ImprovementCheckingRefiner<int>(new DecrementingRefiner()) { Evaluator = new ProblemEvaluator<int>() };
        var algorithm = new AdditiveStepAlgorithm(5)
        {
            Evaluator = new RefinementEvaluator<int>(refiner) { Evaluator = new ProblemEvaluator<int>() }
        };
        var best = Analyzer.BestSoFar<int>(retention: TraceRetention.EveryObservation());
        var run = algorithm.CreateRun(Problem, RandomNumberGenerator.Create(1), [best]);

        run.Complete(cancellationToken: TestContext.Current.CancellationToken);

        run.Result(best).Select(point => point.Candidate).ShouldBe([5, 4, 4]);
        run.Result(best).Select(point => point.Value[0]).ShouldBe([5d, 4d, 4d]);
        run.Result(best).Select(point => point.Evaluations).ShouldBe([1L, 2L, 3L]);
        run.Result(best.Evaluations).CurrentCount.ShouldBe(3);
    }

    [Fact]
    public void DefaultSelection_IgnoresEvaluatorsInSettingsAndDelegates()
    {
        IOperator<Evaluation, int> hidden = new ProblemEvaluator<int>();
        var outer = new HiddenEvaluator(hidden, () => new ProblemEvaluator<int>());
        var best = Analyzer.BestSoFar<int>(retention: TraceRetention.EveryObservation());
        var run = new AdditiveStepAlgorithm(5) { Evaluator = outer }
            .CreateRun(Problem, RandomNumberGenerator.Create(1), [best]);

        run.Complete(cancellationToken: TestContext.Current.CancellationToken);

        var point = run.Result(best).Single();
        point.Candidate.ShouldBe(5);
        point.Value.ShouldBe(new ObjectiveVector(5));
        point.Evaluations.ShouldBe(1);
        run.Result(best.Evaluations).CurrentCount.ShouldBe(1);
    }

    [Fact]
    public void DefaultSelection_IgnoresDescendantsOfAnotherCandidateType()
    {
        var outer = new OtherCandidateEvaluator(new ProblemEvaluator<string>());
        var best = Analyzer.BestSoFar<int>(retention: TraceRetention.EveryObservation());
        var run = new AdditiveStepAlgorithm(5) { Evaluator = outer }
            .CreateRun(Problem, RandomNumberGenerator.Create(1), [best]);

        run.Complete(cancellationToken: TestContext.Current.CancellationToken);

        var point = run.Result(best).Single();
        point.Candidate.ShouldBe(5);
        point.Value.ShouldBe(new ObjectiveVector(5));
        point.Evaluations.ShouldBe(1);
        run.Result(best.Evaluations).CurrentCount.ShouldBe(1);
    }

    [Fact]
    public void BestSoFar_RetainsEveryImprovementAndItsOriginalCandidate()
    {
        RealVector[] candidates = [[5d], [5d], [7d], [3d], [3d], [1d]];
        var problem = FuncProblem.Create((RealVector candidate) => candidate[0], DummySearchSpace<RealVector>.Instance, SingleObjective.Minimize);
        IOperator<Evaluation, RealVector> evaluator = new ProblemEvaluator<RealVector>();
        var best = Analyzer.BestSoFar(evaluator);
        var scope = ResolutionScope.Create();
        var executor = scope.For<RealVector, DummySearchSpace<RealVector>, IProblem<RealVector, DummySearchSpace<RealVector>>>(problem)
            .Resolve(evaluator.Substitute(best));

        foreach (var candidate in candidates)
            executor.Evaluate([candidate], RandomNumberGenerator.Create(1));

        var points = scope.Results.Result(best).ToArray();
        points.Select(point => point.Value[0]).ShouldBe([5d, 3d, 1d]);
        points.Select(point => point.Evaluations).ShouldBe([1L, 4L, 6L]);
        points[0].Candidate.ShouldBeSameAs(candidates[0]);
        points[1].Candidate.ShouldBeSameAs(candidates[3]);
        points[2].Candidate.ShouldBeSameAs(candidates[5]);
    }

    [Fact]
    public void DefaultSelectionChecksDescendantsBeyondDirectChildren()
    {
        IOperator<Evaluation, int> evaluator = new ProblemEvaluator<int>();
        var outer = new OuterEvaluator(new Holder(evaluator));
        var algorithm = new AdditiveStepAlgorithm(1) { Evaluator = outer };
        var best = Analyzer.BestSoFar<int>(retention: TraceRetention.EveryObservation());
        var run = algorithm.CreateRun(Problem, RandomNumberGenerator.Create(1), [best]);
        run.Complete(cancellationToken: TestContext.Current.CancellationToken);
        run.Result(best).Single().Evaluations.ShouldBe(1);
    }

    [Fact]
    public void CurveSpansCycleTurnsAndHonorsItsComparer()
    {
        var algorithm = new AdditiveStepAlgorithm(1);
        var other = new AdditiveStepAlgorithm(10);
        var comparer = Comparer<ObjectiveVector>.Create((left, right) => right[0].CompareTo(left[0]));
        var best = Analyzer.BestSoFar<int>(objectiveComparer: comparer);
        var named = Analyzer.BestSoFar(algorithm.Evaluator, retention: TraceRetention.EveryObservation());
        var count = new Counter();
        var cycle = CycleAlgorithm.Create(algorithm, other) with { MaximumCycles = 3 };
        var run = cycle.CreateRun(Problem, RandomNumberGenerator.Create(1), [best, named, algorithm.Evaluator.Invocations.CountingCandidates(count)]);
        run.Complete(cancellationToken: TestContext.Current.CancellationToken);
        run.Result(best).Select(point => point.Evaluations).ShouldBe([1L, 2L, 3L, 4L, 5L, 6L]);
        run.Result(best).Select(point => point.Value[0]).ShouldBeInOrder();
        run.Result(named).Count.ShouldBe(3);
        run.Result(named).Latest!.Value.Evaluations.ShouldBe(run.Result(count).CurrentCount);
        run.Result(best).Latest!.Value.Candidate.ShouldBe(33);
    }

    [Fact]
    public void AnEmptyEvaluationBeforeTheFirstCandidateRecordsNothing()
    {
        IOperator<Evaluation, int> evaluator = new ProblemEvaluator<int>();
        var best = Analyzer.BestSoFar(evaluator, retention: TraceRetention.EveryObservation());
        var scope = ResolutionScope.Create();
        var record = best.CreateRecorder(scope.Results);

        record(new EvaluationObservation<int>(evaluator, [], [], Problem.SearchSpace, Problem));
        record(new EvaluationObservation<int>(evaluator, [4], [new ObjectiveVector(4)], Problem.SearchSpace, Problem));

        scope.Results.Result(best).Select(point => point.Candidate).ShouldBe([4]);
    }

    [Fact]
    public async Task ConcurrentBestSoFarRecorders_PreserveTheRunningBest()
    {
        IOperator<Evaluation, int> evaluator = new ProblemEvaluator<int>();
        var best = Analyzer.BestSoFar(evaluator, retention: TraceRetention.EveryObservation());
        var results = ResolutionScope.Create().Results;
        var curve = results.Result(best);
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstRound = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var resume = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var ready = 0;
        var writers = Enumerable.Range(0, 4).Select(worker =>
        {
            var record = best.CreateRecorder(results);
            return Task.Run(async () =>
            {
                await start.Task;
                for (var i = worker; i < 200; i += 4)
                {
                    record(new EvaluationObservation<int>(evaluator, [i], [new ObjectiveVector(200 - i)], Problem.SearchSpace, Problem));
                    if (i == worker)
                    {
                        if (Interlocked.Increment(ref ready) == 4)
                            firstRound.SetResult();
                        await resume.Task;
                    }
                }
            }, TestContext.Current.CancellationToken);
        }).ToArray();
        var reading = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var reader = Task.Run(async () =>
        {
            using var snapshot = curve.GetEnumerator();
            reading.SetResult();
            await start.Task;
            await firstRound.Task;
            curve.Count.ShouldBe(4);
            foreach (var point in curve)
                point.Value.ShouldBe(new ObjectiveVector(200 - point.Candidate));
            resume.SetResult();
            while (!writers.All(writer => writer.IsCompleted))
            {
                foreach (var point in curve)
                    point.Value.ShouldBe(new ObjectiveVector(200 - point.Candidate));
                await Task.Yield();
            }
            snapshot.MoveNext().ShouldBeFalse();
        }, TestContext.Current.CancellationToken);
        await reading.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        start.SetResult();

        await Task.WhenAll([.. writers, reader]).WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        var points = curve.ToArray();
        points.Length.ShouldBe(200);
        foreach (var point in points)
            point.Value.ShouldBe(new ObjectiveVector(200 - point.Candidate));
        points.Select(point => point.Candidate).ShouldBeInOrder();
        curve.Latest!.Value.Candidate.ShouldBe(199);
        curve.Latest.Value.Value.ShouldBe(new ObjectiveVector(1));
    }

    [Fact]
    public async Task MutuallyContainingGroups_ConcurrentFirstExpansionFailsWithoutDeadlock()
    {
        using var barrier = new Barrier(2);
        var first = new SynchronizedGroup(barrier);
        var second = new SynchronizedGroup(barrier);
        first.Parts = [second];
        second.Parts = [first];
        var workers = new[] { first, second }.Select(group => Task.Factory.StartNew(
            () => Record.Exception(() => group.Expand()), TestContext.Current.CancellationToken,
            TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray();

        var errors = await Task.WhenAll(workers).WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        foreach (var error in errors)
        {
            var recursive = error.ShouldBeOfType<InvalidOperationException>();
            recursive.Message.ShouldContain(nameof(SynchronizedGroup));
            recursive.Message.ShouldContain("contains itself");
        }
    }

    [Fact]
    public void TraceEnumeration_KeepsItsOriginalSnapshot()
    {
        IOperator<Evaluation, int> evaluator = new ProblemEvaluator<int>();
        var trace = evaluator.Invocations.Trace(observation => observation.Candidates.Single());
        var scope = ResolutionScope.Create();
        var executor = Resolve(scope, evaluator.Substitute(trace));
        executor.Evaluate([5], RandomNumberGenerator.Create(1));
        using var original = scope.Results.Result(trace).GetEnumerator();

        executor.Evaluate([3], RandomNumberGenerator.Create(1));
        executor.Evaluate([1], RandomNumberGenerator.Create(1));

        original.MoveNext().ShouldBeTrue();
        original.Current.Value.ShouldBe(5);
        original.MoveNext().ShouldBeFalse();
        scope.Results.Result(trace).Select(entry => entry.Value).ShouldBe([5, 3, 1]);
    }

    [Fact]
    public void BestSoFarEnumeration_KeepsItsOriginalSnapshot()
    {
        IOperator<Evaluation, int> evaluator = new ProblemEvaluator<int>();
        var best = Analyzer.BestSoFar(evaluator);
        var scope = ResolutionScope.Create();
        var executor = Resolve(scope, evaluator.Substitute(best));
        executor.Evaluate([5], RandomNumberGenerator.Create(1));
        using var original = scope.Results.Result(best).GetEnumerator();

        executor.Evaluate([3], RandomNumberGenerator.Create(1));
        executor.Evaluate([1], RandomNumberGenerator.Create(1));

        original.MoveNext().ShouldBeTrue();
        original.Current.Candidate.ShouldBe(5);
        original.MoveNext().ShouldBeFalse();
        scope.Results.Result(best).Select(point => point.Candidate).ShouldBe([5, 3, 1]);
    }

    [Fact]
    public void UnselectedClocks_AreUnavailable()
    {
        var algorithm = new AdditiveStepAlgorithm(1);
        var best = Analyzer.BestSoFar<int>();
        var trace = algorithm.Evaluator.Invocations.Trace(observation => observation.Candidates.Count);
        var omitted = new Counter();
        var run = algorithm.CreateRun(Problem, RandomNumberGenerator.Create(1), [best, trace]);

        run.Complete(cancellationToken: TestContext.Current.CancellationToken);

        var point = run.Result(best).Single();
        point.Evaluations.ShouldBe(1);
        Should.Throw<InvalidOperationException>(() => point.At(Clock.Elapsed)).Message.ShouldBe("The trace does not use the requested clock.");
        Should.Throw<InvalidOperationException>(() => point.At(omitted.Clock)).Message.ShouldBe("The trace does not use the requested clock.");
        var entry = run.Result(trace).Single();
        Should.Throw<InvalidOperationException>(() => entry.At(Clock.Elapsed)).Message.ShouldBe("The trace does not use the requested clock.");
        Should.Throw<InvalidOperationException>(() => entry.At(omitted.Clock)).Message.ShouldBe("The trace does not use the requested clock.");
    }

    [Fact]
    public void TraceProjection_RejectsAnUnselectedClockWithTheSameMessage()
    {
        IOperator<Evaluation, int> evaluator = new ProblemEvaluator<int>();
        var trace = evaluator.Invocations.Trace(observation => observation.Candidates.Count);
        var omitted = new Counter();
        var scope = ResolutionScope.Create();
        var executor = Resolve(scope, evaluator.Substitute(trace));
        var result = scope.Results.Result(trace);
        var empty = result.Snapshot();

        Should.Throw<InvalidOperationException>(() => empty.By(omitted.Clock)).Message.ShouldBe("The trace does not use the requested clock.");
        Should.Throw<InvalidOperationException>(() => result.By(omitted.Clock)).Message.ShouldBe("The trace does not use the requested clock.");
        executor.Evaluate([1], RandomNumberGenerator.Create(1));
        var populated = result.Snapshot();
        Should.Throw<InvalidOperationException>(() => populated.By(omitted.Clock)).Message.ShouldBe("The trace does not use the requested clock.");
        Should.Throw<InvalidOperationException>(() => result.By(omitted.Clock)).Message.ShouldBe("The trace does not use the requested clock.");
    }

    [Fact]
    public void BestSoFarLatestOnly_KeepsTheLatestObservationAndItsCoordinates()
    {
        IOperator<Evaluation, int> evaluator = new ProblemEvaluator<int>();
        var clock = new Counter();
        var best = Analyzer.BestSoFar(evaluator, clocks: [clock.Clock], retention: TraceRetention.LatestOnly());
        var scope = ResolutionScope.Create();
        var executor = Resolve(scope, evaluator.Substitute(best));

        foreach (var candidate in new[] { 5, 3, 7, 6 })
        {
            scope.Results.Result(clock).IncrementBy(10);
            executor.Evaluate([candidate], RandomNumberGenerator.Create(1));
        }

        var point = scope.Results.Result(best).Single();
        point.Candidate.ShouldBe(3);
        point.Value.ShouldBe(new ObjectiveVector(3));
        point.Evaluations.ShouldBe(4);
        point.At(clock.Clock).ShouldBe(40);
    }

    [Fact]
    public void ASingleSubstitution_ExpandsToItself()
    {
        var substitution = Substitution.Of<Counter>(counter => counter);
        var expanded = substitution.Expand();

        expanded.Single().ShouldBeSameAs(substitution);
        for (var i = 0; i < 3; i++)
        {
            substitution.Expand().ShouldBeSameAs(expanded);
            substitution.Expand().Single().ShouldBeSameAs(substitution);
        }
    }

    [Fact]
    public void GroupsContainingEachOtherFail()
    {
        var first = new Group([]);
        var second = new Group([first]);
        first.Parts = [second];
        Should.Throw<InvalidOperationException>(() => first.Expand()).Message.ShouldContain(nameof(Group));
        Should.Throw<InvalidOperationException>(() => second.Expand()).Message.ShouldContain(nameof(Group));
    }

    private sealed class Group(IReadOnlyList<ISubstitution> parts) : SubstitutionGroup
    {
        public IReadOnlyList<ISubstitution> Parts { get; set; } = parts;
        protected override IReadOnlyList<ISubstitution> CreateSubstitutions() => Parts;
    }

    private sealed class SynchronizedGroup(Barrier barrier) : SubstitutionGroup
    {
        private int expansions;
        public IReadOnlyList<ISubstitution> Parts { get; set; } = [];
        protected override IReadOnlyList<ISubstitution> CreateSubstitutions()
        {
            if (Interlocked.Increment(ref expansions) == 1)
                barrier.SignalAndWait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken).ShouldBeTrue();
            return Parts;
        }
    }

    private sealed record DecrementingRefiner : StatelessRefiner<int>
    {
        public override IReadOnlyList<int> Refine(IReadOnlyList<int> candidates, IRandomNumberGenerator random) =>
            [.. candidates.Select(candidate => candidate - 1)];
    }

    // These evaluators are data and a factory, not declared children; both do work that must stay outside the curve.
    private sealed record HiddenEvaluator(IOperator<Evaluation, int> Setting, Func<IOperator<Evaluation, int>> Factory) : Evaluator<int>
    {
        private static readonly SettingParameter<HiddenEvaluator, IOperator<Evaluation, int>> SettingParameter =
            new(nameof(Setting), node => node.Setting, (node, value) => node with { Setting = value });
        private static readonly Parameter<HiddenEvaluator, Func<IOperator<Evaluation, int>>> FactoryParameter =
            new(nameof(Factory), node => node.Factory, (node, value) => node with { Factory = value });
        public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), SettingParameter, FactoryParameter];
        protected override IEvaluationExecutor<int> CreateExecutor<TSearchState>(
            ResolutionScope<int, ISearchSpace<int>, IProblem<int, ISearchSpace<int>>, TSearchState> scope) =>
            new Executor(scope.Resolve(Setting), scope.Resolve(Factory()));

        private sealed class Executor(IEvaluationExecutor<int> setting, IEvaluationExecutor<int> factory) : IEvaluationExecutor<int>
        {
            public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<int> candidates, IRandomNumberGenerator random)
            {
                setting.Evaluate([-100, -200], random);
                factory.Evaluate([-300], random);
                return [.. candidates.Select(candidate => new ObjectiveVector(candidate))];
            }
        }
    }

    private sealed record OtherCandidateEvaluator(IOperator<Evaluation, string> Evaluator) : Evaluator<int>
    {
        private static readonly Parameter<OtherCandidateEvaluator, IOperator<Evaluation, string>> EvaluatorParameter =
            new(nameof(Evaluator), node => node.Evaluator, (node, value) => node with { Evaluator = value })
            {
                ChildExecution = _ => ExecutionSignature.For<DummySearchSpace<string>, IProblem<string, DummySearchSpace<string>>, PopulationState<string>>()
            };
        public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), EvaluatorParameter];
        protected override IEvaluationExecutor<int> CreateExecutor<TSearchState>(
            ResolutionScope<int, ISearchSpace<int>, IProblem<int, ISearchSpace<int>>, TSearchState> scope)
        {
            var problem = FuncProblem.Create((string _) => -100d, DummySearchSpace<string>.Instance, SingleObjective.Minimize);
            var evaluator = scope.For<string, DummySearchSpace<string>, IProblem<string, DummySearchSpace<string>>, PopulationState<string>>(problem).Resolve(Evaluator);
            return new Executor(evaluator);
        }

        private sealed class Executor(IEvaluationExecutor<string> evaluator) : IEvaluationExecutor<int>
        {
            public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<int> candidates, IRandomNumberGenerator random)
            {
                evaluator.Evaluate(["hidden", "work"], random);
                return [.. candidates.Select(candidate => new ObjectiveVector(candidate))];
            }
        }
    }

    private sealed record UnsupportedEvaluator : IOperator<Evaluation, int>
    {
        public bool Supports(ExecutionSignature execution) => false;
        public IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(ResolutionScope<int, TSearchSpace, TProblem, TSearchState> scope)
            where TSearchSpace : class, ISearchSpace<int>
            where TProblem : class, IProblem<int, TSearchSpace>
            where TSearchState : class, ISearchState => throw new InvalidOperationException();
    }

    private sealed record Holder(IOperator<Evaluation, int> Evaluator) : IConfigurationNode
    {
        private static readonly Parameter<Holder, IOperator<Evaluation, int>> EvaluatorParameter =
            new(nameof(Evaluator), node => node.Evaluator, (node, value) => node with { Evaluator = value });
        public IReadOnlyList<IParameter> GetParameters() => [EvaluatorParameter];
    }

    private sealed record OuterEvaluator(Holder Holder) : Evaluator<int>
    {
        private static readonly Parameter<OuterEvaluator, Holder> HolderParameter =
            new(nameof(Holder), node => node.Holder, (node, value) => node with { Holder = value });
        public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), HolderParameter];
        protected override IEvaluationExecutor<int> CreateExecutor<TSearchState>(
            ResolutionScope<int, ISearchSpace<int>, IProblem<int, ISearchSpace<int>>, TSearchState> scope) => scope.Resolve(Holder.Evaluator);
    }
}
