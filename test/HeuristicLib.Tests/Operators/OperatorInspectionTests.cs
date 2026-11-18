using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Replacers;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Operators;

public class OperatorInspectionTests
{
    [Theory]
    [InlineData(OperatorCountMetric.Invocations, 4)]
    [InlineData(OperatorCountMetric.Candidates, 7)]
    public void CountingMutator_NewExecutorsShareOnlyTheCounterOfTheirRun(OperatorCountMetric metric, int sharedCount)
    {
        var preparations = 0;
        var counter = new Counter();
        var observedCalls = new Counter();
        var leaf = new AdvancingMutator(() => preparations++);
        var source = leaf.Counted(counter, metric);
        var random = RandomNumberGenerator.Create(1);
        var parent = ResolutionScope.Create();
        var outer = parent.ResolveMutator(source);
        outer.Mutate([0, 0], random).ShouldBe([1, 1]);

        var child = parent.CreateChildScope();
        var inner = child.ResolveMutator(source.Substitute(leaf.Invocations.Counting(observedCalls)));
        inner.ShouldNotBeSameAs(outer);
        inner.Mutate([0], random).ShouldBe([1]);
        outer.Mutate([0, 0, 0], random).ShouldBe([2, 2, 2]);
        inner.Mutate([0], random).ShouldBe([2]);

        parent.Results.Result(counter).CurrentCount.ShouldBe(sharedCount);
        parent.Results.Result(observedCalls).CurrentCount.ShouldBe(2);
        preparations.ShouldBe(2);

        var independentScope = ResolutionScope.Create();
        var independent = independentScope.ResolveMutator(source);
        independent.Mutate([0], random).ShouldBe([1]);
        parent.Results.Result(counter).CurrentCount.ShouldBe(sharedCount);
        independentScope.Results.Result(counter).CurrentCount.ShouldBe(1);
        parent.Results.Result(observedCalls).CurrentCount.ShouldBe(2);
        preparations.ShouldBe(3);
    }

    // A child scope builds new executors, which add to the one duration of the run. An independent run has its own.
    [Fact]
    public void TimedMutator_NewExecutorsShareOnlyTheDurationOfTheirRun()
    {
        var preparations = 0;
        var observedCalls = new Counter();
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var duration = new Duration();
        var leaf = new AdvancingMutator(() => preparations++);
        var source = leaf.Timed(duration);
        var random = RandomNumberGenerator.Create(1);
        var parent = ResolutionScope.Create(timeProvider);
        var measured = parent.Results.Result(duration);
        var outer = parent.ResolveMutator(source);
        measured.CurrentDuration.ShouldBe(TimeSpan.Zero);
        outer.Mutate([0], random).ShouldBe([1]);
        measured.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(3));

        var child = parent.CreateChildScope();
        var inner = child.ResolveMutator(source.Substitute(leaf.Invocations.Counting(observedCalls)));
        inner.ShouldNotBeSameAs(outer);
        measured.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(3));
        inner.Mutate([0], random).ShouldBe([1]);
        measured.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(6));
        outer.Mutate([0], random).ShouldBe([2]);
        inner.Mutate([0], random).ShouldBe([2]);

        measured.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(12));
        parent.Results.Result(observedCalls).CurrentCount.ShouldBe(2);
        preparations.ShouldBe(2);

        var independentScope = ResolutionScope.Create(timeProvider);
        var independent = independentScope.ResolveMutator(source);
        independent.Mutate([0], random).ShouldBe([1]);
        measured.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(12));
        independentScope.Results.Result(duration).CurrentDuration.ShouldBe(TimeSpan.FromSeconds(3));
        parent.Results.Result(observedCalls).CurrentCount.ShouldBe(2);
        preparations.ShouldBe(3);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void TimedMutator_PropagatesTheOperationOrTimerFailure(bool failOperation, bool failTimer)
    {
        var operationFailure = new InvalidOperationException("operation");
        var timerFailure = new InvalidOperationException("timer");
        var calls = 0;
        var timestamps = 0;
        var source = new CallbackMutator((parents, _, _, _) =>
        {
            calls++;
            return failOperation ? throw operationFailure : parents;
        });
        var time = new CallbackTimeProvider(() => ++timestamps == 2 && failTimer ? throw timerFailure : timestamps);
        var duration = new Duration();
        var scope = ResolutionScope.Create(time);
        var executor = scope.ResolveMutator(source.Timed(duration));

        var failure = Record.Exception(() => executor.Mutate([1], RandomNumberGenerator.Create(1)));

        // A timer failure while recording is thrown from finally and therefore replaces an operation failure.
        failure.ShouldBeSameAs(failTimer ? timerFailure : operationFailure);
        calls.ShouldBe(1);
        timestamps.ShouldBe(2);
    }

    [Fact]
    public void TimedMutator_WhenStartingTheTimerFails_DoesNotCallTheOperationOrFinishTheTimer()
    {
        var failure = new InvalidOperationException("timer start");
        var calls = 0;
        var timestamps = 0;
        var source = new CallbackMutator((parents, _, _, _) => { calls++; return parents; });
        var time = new CallbackTimeProvider(() => { timestamps++; throw failure; });
        var duration = new Duration();
        var scope = ResolutionScope.Create(time);
        var executor = scope.ResolveMutator(source.Timed(duration));

        Record.Exception(() => executor.Mutate([1], RandomNumberGenerator.Create(1)))
            .ShouldBeSameAs(failure);

        calls.ShouldBe(0);
        timestamps.ShouldBe(1);
        scope.Results.Result(duration).CurrentDuration.ShouldBe(TimeSpan.Zero);
    }

    [Fact]
    public void CountCreatorCalls_IncrementsOncePerCreateCall()
    {
        var counter = new Counter();
        var creator = new SequenceCreator();
        var counted = creator.Counted(counter);
        counted.Counter.ShouldBeSameAs(counter);
        counted.Metric.ShouldBe(OperatorCountMetric.Invocations);
        var scope = ResolutionScope.Create();
        var executor = scope.ResolveCreator(counted);

        executor.Create(3, RandomNumberGenerator.Create(1));
        executor.Create(1, RandomNumberGenerator.Create(2));

        scope.Results.Result(counter).CurrentCount.ShouldBe(2);
    }

    [Fact]
    public void CountCreatedCandidates_IncrementsByReturnedCandidateCount()
    {
        var counter = new Counter();
        var creator = new SequenceCreator();
        var counted = creator.Counted(counter, OperatorCountMetric.Candidates);
        var scope = ResolutionScope.Create();
        var executor = scope.ResolveCreator(counted);

        executor.Create(3, RandomNumberGenerator.Create(1));
        executor.Create(1, RandomNumberGenerator.Create(2));

        scope.Results.Result(counter).CurrentCount.ShouldBe(4);
    }

    [Fact]
    public void MeasureCreatorDuration_AddsElapsedCreatorExecutionDuration()
    {
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var duration = new Duration();
        var creator = new SequenceCreator();
        var scope = ResolutionScope.Create(timeProvider);
        var executor = scope.ResolveCreator(creator.Timed(duration));

        executor.Create(3, RandomNumberGenerator.Create(1));
        executor.Create(1, RandomNumberGenerator.Create(2));

        scope.Results.Result(duration).CurrentDuration.ShouldBe(TimeSpan.FromSeconds(6));
    }

    [Fact]
    public void CountCreatorCalls_DoesNotIncrementWhenCreationThrows()
    {
        var counter = new Counter();
        var source = new ThrowingCreator();
        var scope = ResolutionScope.Create();
        var executor = scope.ResolveCreator(source.Counted(counter));

        Should.Throw<InvalidOperationException>(() =>
            executor.Create(1, RandomNumberGenerator.Create(1)));

        scope.Results.Result(counter).CurrentCount.ShouldBe(0);
    }

    [Fact]
    public void MeasureCreatorDuration_RecordsElapsedDurationWhenCreationThrows()
    {
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var duration = new Duration();
        var source = new ThrowingCreator();
        var scope = ResolutionScope.Create(timeProvider);
        var executor = scope.ResolveCreator(source.Timed(duration));

        Should.Throw<InvalidOperationException>(() =>
            executor.Create(1, RandomNumberGenerator.Create(1)));

        scope.Results.Result(duration).CurrentDuration.ShouldBe(TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void CountMutatorCalls_IncrementsOncePerMutateCall()
    {
        var counter = new Counter();
        var mutator = new AddOneMutator();
        var counted = mutator.Counted(counter);
        counted.Counter.ShouldBeSameAs(counter);
        counted.Metric.ShouldBe(OperatorCountMetric.Invocations);
        var scope = ResolutionScope.Create();
        var executor = scope.ResolveMutator(counted);

        executor.Mutate([1, 2, 3], RandomNumberGenerator.Create(1));
        executor.Mutate([4], RandomNumberGenerator.Create(2));

        scope.Results.Result(counter).CurrentCount.ShouldBe(2);
    }

    [Fact]
    public void CountMutatedCandidates_IncrementsByReturnedCandidateCount()
    {
        var counter = new Counter();
        var mutator = new AddOneMutator();
        var counted = mutator.Counted(counter, OperatorCountMetric.Candidates);
        var scope = ResolutionScope.Create();
        var executor = scope.ResolveMutator(counted);

        executor.Mutate([1, 2, 3], RandomNumberGenerator.Create(1));
        executor.Mutate([4], RandomNumberGenerator.Create(2));

        scope.Results.Result(counter).CurrentCount.ShouldBe(4);
    }

    [Fact]
    public void CountMutatorCalls_DoesNotIncrementWhenMutationThrows()
    {
        var counter = new Counter();
        var source = new ThrowingMutator();
        var scope = ResolutionScope.Create();
        var executor = scope.ResolveMutator(source.Counted(counter));

        Should.Throw<InvalidOperationException>(() =>
            executor.Mutate([1], RandomNumberGenerator.Create(1)));

        scope.Results.Result(counter).CurrentCount.ShouldBe(0);
    }

    [Fact]
    public void MeasureMutatorDuration_AddsElapsedMutatorExecutionDuration()
    {
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var duration = new Duration();
        var mutator = new AddOneMutator();
        var scope = ResolutionScope.Create(timeProvider);
        var executor = scope.ResolveMutator(mutator.Timed(duration));

        executor.Mutate([1, 2, 3], RandomNumberGenerator.Create(1));
        executor.Mutate([4], RandomNumberGenerator.Create(2));

        scope.Results.Result(duration).CurrentDuration.ShouldBe(TimeSpan.FromSeconds(6));
    }

    [Fact]
    public void MeasureMutatorDuration_RecordsElapsedDurationWhenMutationThrows()
    {
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var duration = new Duration();
        var mutator = new ThrowingMutator();
        var scope = ResolutionScope.Create(timeProvider);
        var executor = scope.ResolveMutator(mutator.Timed(duration));

        Should.Throw<InvalidOperationException>(() =>
            executor.Mutate([1], RandomNumberGenerator.Create(1)));

        scope.Results.Result(duration).CurrentDuration.ShouldBe(TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void NestedTimedMutators_ForwardTheSameInvocationDataAndInvokeTheChildMutatorOnce()
    {
        var calls = 0;
        IReadOnlyList<int> parents = [1, 2, 3];
        var random = RandomNumberGenerator.Create(1);
        var problem = CreateProblem();
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var innerDuration = new Duration();
        var outerDuration = new Duration();
        var childMutator = new CallbackMutator((actualParents, actualRandom, actualSearchSpace, actualProblem) =>
        {
            calls++;
            actualParents.ShouldBeSameAs(parents);
            actualRandom.ShouldBeSameAs(random);
            actualSearchSpace.ShouldBeSameAs(problem.SearchSpace);
            actualProblem.ShouldBeSameAs(problem);
            return actualParents;
        });
        var scope = ResolutionScope.Create(timeProvider);
        var executor = scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(childMutator.Timed(innerDuration).Timed(outerDuration), problem);

        var result = executor.Mutate(parents, random);

        result.ShouldBeSameAs(parents);
        calls.ShouldBe(1);
        scope.Results.Result(innerDuration).CurrentDuration.ShouldBe(TimeSpan.FromSeconds(3));
        scope.Results.Result(outerDuration).CurrentDuration.ShouldBe(TimeSpan.FromSeconds(9));
    }

    [Fact]
    public void NestedTimedMutators_RecordEveryDurationWhenTheChildMutatorThrows()
    {
        var calls = 0;
        var problem = CreateProblem();
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var innerDuration = new Duration();
        var outerDuration = new Duration();
        var childMutator = new CallbackMutator((_, _, _, _) =>
        {
            calls++;
            throw new InvalidOperationException();
        });
        var scope = ResolutionScope.Create(timeProvider);
        var executor = scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(childMutator.Timed(innerDuration).Timed(outerDuration), problem);

        Should.Throw<InvalidOperationException>(() =>
            executor.Mutate([1], RandomNumberGenerator.Create(1)));

        calls.ShouldBe(1);
        scope.Results.Result(innerDuration).CurrentDuration.ShouldBe(TimeSpan.FromSeconds(3));
        scope.Results.Result(outerDuration).CurrentDuration.ShouldBe(TimeSpan.FromSeconds(9));
    }

    [Fact]
    public void CountCrossoverCalls_IncrementsOncePerCrossCall()
    {
        var counter = new Counter();
        var crossover = new SumParentsCrossover();
        var counted = crossover.Counted(counter);
        counted.Counter.ShouldBeSameAs(counter);
        counted.Metric.ShouldBe(OperatorCountMetric.Invocations);
        var scope = ResolutionScope.Create();
        var executor = scope.ResolveCrossover(counted);

        executor.Cross(
            [Parents.From(1, 10), Parents.From(2, 20), Parents.From(3, 30)],
            RandomNumberGenerator.Create(1));
        executor.Cross(
            [Parents.From(4, 40)],
            RandomNumberGenerator.Create(2));

        scope.Results.Result(counter).CurrentCount.ShouldBe(2);
    }

    [Fact]
    public void CountCrossedCandidates_IncrementsByReturnedCandidateCount()
    {
        var counter = new Counter();
        var crossover = new SumParentsCrossover();
        var counted = crossover.Counted(counter, OperatorCountMetric.Candidates);
        var scope = ResolutionScope.Create();
        var executor = scope.ResolveCrossover(counted);

        executor.Cross(
            [Parents.From(1, 10), Parents.From(2, 20), Parents.From(3, 30)],
            RandomNumberGenerator.Create(1));
        executor.Cross(
            [Parents.From(4, 40)],
            RandomNumberGenerator.Create(2));

        scope.Results.Result(counter).CurrentCount.ShouldBe(4);
    }

    [Fact]
    public void MeasureCrossoverDuration_AddsElapsedCrossoverExecutionDuration()
    {
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var duration = new Duration();
        var crossover = new SumParentsCrossover();
        var scope = ResolutionScope.Create(timeProvider);
        var executor = scope.ResolveCrossover(crossover.Timed(duration));

        executor.Cross(
            [Parents.From(1, 10), Parents.From(2, 20), Parents.From(3, 30)],
            RandomNumberGenerator.Create(1));
        executor.Cross(
            [Parents.From(4, 40)],
            RandomNumberGenerator.Create(2));

        scope.Results.Result(duration).CurrentDuration.ShouldBe(TimeSpan.FromSeconds(6));
    }

    [Fact]
    public void CountCrossoverCalls_DoesNotIncrementWhenCrossoverThrows()
    {
        var counter = new Counter();
        var source = new ThrowingCrossover();
        var scope = ResolutionScope.Create();
        var executor = scope.ResolveCrossover(source.Counted(counter));

        Should.Throw<InvalidOperationException>(() =>
            executor.Cross([Parents.From(1, 10)], RandomNumberGenerator.Create(1)));

        scope.Results.Result(counter).CurrentCount.ShouldBe(0);
    }

    [Fact]
    public void MeasureCrossoverDuration_RecordsElapsedDurationWhenCrossoverThrows()
    {
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var duration = new Duration();
        var source = new ThrowingCrossover();
        var scope = ResolutionScope.Create(timeProvider);
        var executor = scope.ResolveCrossover(source.Timed(duration));

        Should.Throw<InvalidOperationException>(() =>
            executor.Cross([Parents.From(1, 10)], RandomNumberGenerator.Create(1)));

        scope.Results.Result(duration).CurrentDuration.ShouldBe(TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void CountSelectorCalls_IncrementsOncePerSelectCall()
    {
        var counter = new Counter();
        var selector = new FirstCandidatesSelector();
        var counted = selector.Counted(counter);
        counted.Counter.ShouldBeSameAs(counter);
        counted.Metric.ShouldBe(OperatorCountMetric.Invocations);
        var scope = ResolutionScope.Create();
        var executor = scope.ResolveSelector(counted);
        var problem = CreateProblem();

        executor.Select(CreateEvaluatedCandidates([1, 2, 3]), problem.Objective, 2, RandomNumberGenerator.Create(1));
        executor.Select(CreateEvaluatedCandidates([4]), problem.Objective, 1, RandomNumberGenerator.Create(2));

        scope.Results.Result(counter).CurrentCount.ShouldBe(2);
    }

    [Fact]
    public void CountSelectedCandidates_IncrementsByReturnedCandidateCount()
    {
        var counter = new Counter();
        var selector = new FirstCandidatesSelector();
        var counted = selector.Counted(counter, OperatorCountMetric.Candidates);
        var scope = ResolutionScope.Create();
        var executor = scope.ResolveSelector(counted);
        var problem = CreateProblem();

        executor.Select(CreateEvaluatedCandidates([1, 2, 3]), problem.Objective, 2, RandomNumberGenerator.Create(1));
        executor.Select(CreateEvaluatedCandidates([4]), problem.Objective, 1, RandomNumberGenerator.Create(2));

        scope.Results.Result(counter).CurrentCount.ShouldBe(3);
    }

    [Fact]
    public void CountSelectorCalls_DoesNotIncrementWhenSelectionThrows()
    {
        var counter = new Counter();
        var source = new ThrowingSelector();
        var scope = ResolutionScope.Create();
        var executor = scope.ResolveSelector(source.Counted(counter));
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() =>
            executor.Select(CreateEvaluatedCandidates([1]), problem.Objective, 1, RandomNumberGenerator.Create(1)));

        scope.Results.Result(counter).CurrentCount.ShouldBe(0);
    }

    [Fact]
    public void MeasureSelectorDuration_AddsElapsedSelectorExecutionDuration()
    {
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var duration = new Duration();
        var selector = new FirstCandidatesSelector();
        var scope = ResolutionScope.Create(timeProvider);
        var executor = scope.ResolveSelector(selector.Timed(duration));
        var problem = CreateProblem();

        executor.Select(CreateEvaluatedCandidates([1, 2, 3]), problem.Objective, 2, RandomNumberGenerator.Create(1));
        executor.Select(CreateEvaluatedCandidates([4]), problem.Objective, 1, RandomNumberGenerator.Create(2));

        scope.Results.Result(duration).CurrentDuration.ShouldBe(TimeSpan.FromSeconds(6));
    }

    [Fact]
    public void MeasureSelectorDuration_RecordsElapsedDurationWhenSelectionThrows()
    {
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var duration = new Duration();
        var selector = new ThrowingSelector();
        var scope = ResolutionScope.Create(timeProvider);
        var executor = scope.ResolveSelector(selector.Timed(duration));
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() =>
            executor.Select(CreateEvaluatedCandidates([1]), problem.Objective, 1, RandomNumberGenerator.Create(1)));

        scope.Results.Result(duration).CurrentDuration.ShouldBe(TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void CountReplacerCalls_IncrementsOncePerReplaceCall()
    {
        var counter = new Counter();
        var replacer = new FirstReplacementCandidatesReplacer();
        var counted = replacer.Counted(counter);
        counted.Counter.ShouldBeSameAs(counter);
        counted.Metric.ShouldBe(OperatorCountMetric.Invocations);
        var scope = ResolutionScope.Create();
        var executor = scope.ResolveReplacer(counted);
        var problem = CreateProblem();

        executor.Replace(
            CreateEvaluatedCandidates([1, 2, 3]),
            CreateEvaluatedCandidates([10, 20]),
            problem.Objective,
            2,
            RandomNumberGenerator.Create(1));
        executor.Replace(
            CreateEvaluatedCandidates([4]),
            CreateEvaluatedCandidates([40]),
            problem.Objective,
            1,
            RandomNumberGenerator.Create(2));

        scope.Results.Result(counter).CurrentCount.ShouldBe(2);
    }

    [Fact]
    public void CountReplacementCandidates_IncrementsByReturnedCandidateCount()
    {
        var counter = new Counter();
        var replacer = new FirstReplacementCandidatesReplacer();
        var counted = replacer.Counted(counter, OperatorCountMetric.Candidates);
        var scope = ResolutionScope.Create();
        var executor = scope.ResolveReplacer(counted);
        var problem = CreateProblem();

        executor.Replace(
            CreateEvaluatedCandidates([1, 2, 3]),
            CreateEvaluatedCandidates([10, 20]),
            problem.Objective,
            2,
            RandomNumberGenerator.Create(1));
        executor.Replace(
            CreateEvaluatedCandidates([4]),
            CreateEvaluatedCandidates([40]),
            problem.Objective,
            1,
            RandomNumberGenerator.Create(2));

        scope.Results.Result(counter).CurrentCount.ShouldBe(3);
    }

    [Fact]
    public void CountReplacerCalls_DoesNotCountFailedCall()
    {
        var counter = new Counter();
        var source = new ThrowingReplacer();
        var scope = ResolutionScope.Create();
        var executor = scope.ResolveReplacer(source.Counted(counter));
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() => executor.Replace(
            CreateEvaluatedCandidates([1]),
            CreateEvaluatedCandidates([2]),
            problem.Objective,
            1,
            RandomNumberGenerator.Create(1)));

        scope.Results.Result(counter).CurrentCount.ShouldBe(0);
    }

    [Fact]
    public void MeasureReplacerDuration_AddsElapsedReplacerExecutionDuration()
    {
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var duration = new Duration();
        var replacer = new FirstReplacementCandidatesReplacer();
        var scope = ResolutionScope.Create(timeProvider);
        var executor = scope.ResolveReplacer(replacer.Timed(duration));
        var problem = CreateProblem();

        executor.Replace(
            CreateEvaluatedCandidates([1, 2, 3]),
            CreateEvaluatedCandidates([10, 20]),
            problem.Objective,
            2,
            RandomNumberGenerator.Create(1));
        executor.Replace(
            CreateEvaluatedCandidates([4]),
            CreateEvaluatedCandidates([40]),
            problem.Objective,
            1,
            RandomNumberGenerator.Create(2));

        scope.Results.Result(duration).CurrentDuration.ShouldBe(TimeSpan.FromSeconds(6));
    }

    [Fact]
    public void MeasureReplacerDuration_RecordsFailedCall()
    {
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var duration = new Duration();
        var source = new ThrowingReplacer();
        var scope = ResolutionScope.Create(timeProvider);
        var executor = scope.ResolveReplacer(source.Timed(duration));
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() => executor.Replace(
            CreateEvaluatedCandidates([1]),
            CreateEvaluatedCandidates([2]),
            problem.Objective,
            1,
            RandomNumberGenerator.Create(1)));

        scope.Results.Result(duration).CurrentDuration.ShouldBe(TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void CountInterceptorCalls_IncrementsOncePerTransformCall()
    {
        var counter = new Counter();
        var interceptor = new AddOneInterceptor();
        var counted = interceptor.Counted(counter);
        counted.Counter.ShouldBeSameAs(counter);
        var scope = ResolutionScope.Create();
        var problem = CreateProblem();
        var executor = scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, CounterState>(counted, problem);

        executor.Transform(new CounterState { Value = 1 }, previousState: null, RandomNumberGenerator.Create(1));
        executor.Transform(new CounterState { Value = 2 }, new CounterState { Value = 1 }, RandomNumberGenerator.Create(2));

        scope.Results.Result(counter).CurrentCount.ShouldBe(2);
    }

    [Fact]
    public void MeasureInterceptorDuration_AddsElapsedInterceptorExecutionDuration()
    {
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var duration = new Duration();
        var interceptor = new AddOneInterceptor();
        var scope = ResolutionScope.Create(timeProvider);
        var problem = CreateProblem();
        var executor = scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, CounterState>(interceptor.Timed(duration), problem);

        executor.Transform(new CounterState { Value = 1 }, previousState: null, RandomNumberGenerator.Create(1));
        executor.Transform(new CounterState { Value = 2 }, new CounterState { Value = 1 }, RandomNumberGenerator.Create(2));

        scope.Results.Result(duration).CurrentDuration.ShouldBe(TimeSpan.FromSeconds(6));
    }

    [Fact]
    public void CountInterceptorCalls_DoesNotCountFailedCall()
    {
        var counter = new Counter();
        var source = new ThrowingInterceptor();
        var scope = ResolutionScope.Create();
        var problem = CreateProblem();
        var executor = scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, CounterState>(source.Counted(counter), problem);

        Should.Throw<InvalidOperationException>(() => executor.Transform(new CounterState { Value = 1 }, null, RandomNumberGenerator.Create(1)));

        scope.Results.Result(counter).CurrentCount.ShouldBe(0);
    }

    [Fact]
    public void MeasureInterceptorDuration_RecordsFailedCall()
    {
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var duration = new Duration();
        var source = new ThrowingInterceptor();
        var scope = ResolutionScope.Create(timeProvider);
        var problem = CreateProblem();
        var executor = scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, CounterState>(source.Timed(duration), problem);

        Should.Throw<InvalidOperationException>(() => executor.Transform(new CounterState { Value = 1 }, null, RandomNumberGenerator.Create(1)));

        scope.Results.Result(duration).CurrentDuration.ShouldBe(TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void CountTerminatorCalls_IncrementsOncePerTerminalStateCheck()
    {
        var counter = new Counter();
        var terminator = new NeverTerminalStateTerminator();
        var counted = terminator.Counted(counter);
        counted.Counter.ShouldBeSameAs(counter);
        var scope = ResolutionScope.Create();
        var problem = CreateProblem();
        var executor = scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, CounterState>(counted, problem);

        executor.IsTerminalState(new CounterState { Value = 1 }, RandomNumberGenerator.Create(0));
        executor.IsTerminalState(new CounterState { Value = 2 }, RandomNumberGenerator.Create(0));

        scope.Results.Result(counter).CurrentCount.ShouldBe(2);
    }

    [Fact]
    public void MeasureTerminatorDuration_AddsElapsedTerminatorExecutionDuration()
    {
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var duration = new Duration();
        var terminator = new NeverTerminalStateTerminator();
        var scope = ResolutionScope.Create(timeProvider);
        var problem = CreateProblem();
        var executor = scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, CounterState>(terminator.Timed(duration), problem);

        executor.IsTerminalState(new CounterState { Value = 1 }, RandomNumberGenerator.Create(0));
        executor.IsTerminalState(new CounterState { Value = 2 }, RandomNumberGenerator.Create(0));

        scope.Results.Result(duration).CurrentDuration.ShouldBe(TimeSpan.FromSeconds(6));
    }

    [Fact]
    public void CountTerminatorCalls_DoesNotCountFailedCall()
    {
        var counter = new Counter();
        var source = new ThrowingTerminator();
        var scope = ResolutionScope.Create();
        var problem = CreateProblem();
        var executor = scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, CounterState>(source.Counted(counter), problem);

        Should.Throw<InvalidOperationException>(() => executor.IsTerminalState(new CounterState { Value = 1 }, RandomNumberGenerator.Create(0)));

        scope.Results.Result(counter).CurrentCount.ShouldBe(0);
    }

    [Fact]
    public void MeasureTerminatorDuration_RecordsFailedCall()
    {
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var duration = new Duration();
        var source = new ThrowingTerminator();
        var scope = ResolutionScope.Create(timeProvider);
        var problem = CreateProblem();
        var executor = scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, CounterState>(source.Timed(duration), problem);

        Should.Throw<InvalidOperationException>(() => executor.IsTerminalState(new CounterState { Value = 1 }, RandomNumberGenerator.Create(0)));

        scope.Results.Result(duration).CurrentDuration.ShouldBe(TimeSpan.FromSeconds(3));
    }

    private static FuncProblem<int, DummySearchSpace<int>> CreateProblem()
    {
        return FuncProblem.Create(
            evaluateFunc: static (int candidate) => candidate,
            encoding: DummySearchSpace<int>.Instance,
            objective: SingleObjective.Minimize);
    }

    private static IReadOnlyList<EvaluatedCandidate<int>> CreateEvaluatedCandidates(IReadOnlyList<int> candidates)
    {
        return candidates
            .Select(candidate => EvaluatedCandidate.From(candidate, new ObjectiveVector(candidate)))
            .ToArray();
    }

    private sealed record SequenceCreator
        : StatelessCreator<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>
    {
        public override IReadOnlyList<int> Create(
            int count,
            IRandomNumberGenerator random,
            DummySearchSpace<int> searchSpace,
            FuncProblem<int, DummySearchSpace<int>> problem)
        {
            return Enumerable.Range(0, count).ToArray();
        }
    }

    private sealed record AddOneMutator
        : StatelessMutator<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>
    {
        public override IReadOnlyList<int> Mutate(
            IReadOnlyList<int> parents,
            IRandomNumberGenerator random,
            DummySearchSpace<int> searchSpace,
            FuncProblem<int, DummySearchSpace<int>> problem)
        {
            return parents.Select(parent => parent + 1).ToArray();
        }
    }

    private sealed record ThrowingMutator
        : StatelessMutator<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>
    {
        public override IReadOnlyList<int> Mutate(
            IReadOnlyList<int> parents,
            IRandomNumberGenerator random,
            DummySearchSpace<int> searchSpace,
            FuncProblem<int, DummySearchSpace<int>> problem) =>
            throw new InvalidOperationException();
    }

    private delegate IReadOnlyList<int> MutateCallback(
        IReadOnlyList<int> parents,
        IRandomNumberGenerator random,
        DummySearchSpace<int> searchSpace,
        FuncProblem<int, DummySearchSpace<int>> problem);

    private sealed record CallbackMutator(MutateCallback callback)
        : Mutator<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>
    {
        protected override IMutationExecutor<int> CreateExecutor<TSearchState>(ResolutionScope<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, TSearchState> scope) =>
            new Executor(callback, scope.Problem);

        private sealed class Executor(MutateCallback callback, FuncProblem<int, DummySearchSpace<int>> problem)
            : IMutationExecutor<int>
        {
            private readonly DummySearchSpace<int> searchSpace = problem.SearchSpace;

            public IReadOnlyList<int> Mutate(
                IReadOnlyList<int> parents, IRandomNumberGenerator random) =>
                callback(parents, random, searchSpace, problem);
        }
    }

    private sealed record ThrowingCreator
        : StatelessCreator<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>
    {
        public override IReadOnlyList<int> Create(
            int count,
            IRandomNumberGenerator random,
            DummySearchSpace<int> searchSpace,
            FuncProblem<int, DummySearchSpace<int>> problem) =>
            throw new InvalidOperationException();
    }

    private sealed record ThrowingCrossover
        : StatelessCrossover<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>
    {
        public override IReadOnlyList<int> Cross(
            IReadOnlyList<Parents<int>> parents,
            IRandomNumberGenerator random,
            DummySearchSpace<int> searchSpace,
            FuncProblem<int, DummySearchSpace<int>> problem) =>
            throw new InvalidOperationException();
    }

    private sealed record SumParentsCrossover
        : StatelessCrossover<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>
    {
        public override IReadOnlyList<int> Cross(
            IReadOnlyList<Parents<int>> parents,
            IRandomNumberGenerator random,
            DummySearchSpace<int> searchSpace,
            FuncProblem<int, DummySearchSpace<int>> problem)
        {
            return parents.Select(pair => pair.Parent1 + pair.Parent2).ToArray();
        }
    }

    private sealed record FirstCandidatesSelector
        : StatelessSelector<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>
    {
        public override IReadOnlyList<EvaluatedCandidate<int>> Select(
            IReadOnlyList<EvaluatedCandidate<int>> population,
            ObjectiveDirections objective,
            int count,
            IRandomNumberGenerator random,
            DummySearchSpace<int> searchSpace,
            FuncProblem<int, DummySearchSpace<int>> problem)
        {
            return population.Take(count).ToArray();
        }
    }

    private sealed record ThrowingSelector
        : StatelessSelector<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>
    {
        public override IReadOnlyList<EvaluatedCandidate<int>> Select(
            IReadOnlyList<EvaluatedCandidate<int>> population,
            ObjectiveDirections objective,
            int count,
            IRandomNumberGenerator random,
            DummySearchSpace<int> searchSpace,
            FuncProblem<int, DummySearchSpace<int>> problem) =>
            throw new InvalidOperationException();
    }

    private sealed record FirstReplacementCandidatesReplacer
        : StatelessReplacer<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>
    {
        public override IReadOnlyList<EvaluatedCandidate<int>> Replace(
            IReadOnlyList<EvaluatedCandidate<int>> previousPopulation,
            IReadOnlyList<EvaluatedCandidate<int>> offspringPopulation,
            ObjectiveDirections objective,
            int count,
            IRandomNumberGenerator random,
            DummySearchSpace<int> searchSpace,
            FuncProblem<int, DummySearchSpace<int>> problem)
        {
            return previousPopulation.Concat(offspringPopulation).Take(count).ToArray();
        }
    }

    private sealed record ThrowingReplacer
        : StatelessReplacer<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>
    {
        public override IReadOnlyList<EvaluatedCandidate<int>> Replace(
            IReadOnlyList<EvaluatedCandidate<int>> previousPopulation,
            IReadOnlyList<EvaluatedCandidate<int>> offspringPopulation,
            ObjectiveDirections objective,
            int count,
            IRandomNumberGenerator random,
            DummySearchSpace<int> searchSpace,
            FuncProblem<int, DummySearchSpace<int>> problem) =>
            throw new InvalidOperationException();
    }

    private sealed record AddOneInterceptor
        : StatelessInterceptor<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, CounterState>
    {
        public override CounterState Transform(
            CounterState currentState,
            CounterState? previousState,
            IRandomNumberGenerator random,
            DummySearchSpace<int> searchSpace,
            FuncProblem<int, DummySearchSpace<int>> problem)
        {
            return currentState with { Value = currentState.Value + 1 };
        }
    }

    private sealed record ThrowingInterceptor
        : StatelessInterceptor<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, CounterState>
    {
        public override CounterState Transform(CounterState currentState, CounterState? previousState, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace, FuncProblem<int, DummySearchSpace<int>> problem) =>
            throw new InvalidOperationException();
    }

    private sealed record NeverTerminalStateTerminator
        : StatelessTerminator<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, CounterState>
    {
        public override bool IsTerminalState(
            CounterState state,
            IRandomNumberGenerator random,
            DummySearchSpace<int> searchSpace,
            FuncProblem<int, DummySearchSpace<int>> problem)
        {
            return false;
        }
    }

    private sealed record ThrowingTerminator
        : StatelessTerminator<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, CounterState>
    {
        public override bool IsTerminalState(CounterState state, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace, FuncProblem<int, DummySearchSpace<int>> problem) =>
            throw new InvalidOperationException();
    }

    private sealed record AdvancingMutator(Action Prepare) : Mutator<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>
    {

        protected override IMutationExecutor<int> CreateExecutor<TRunSearchState>(ResolutionScope<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, TRunSearchState> scope)
        {
            Prepare();
            return new Executor(scope.Problem);
        }

        private sealed class Executor
            : MutationExecutor<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>
        {
            public Executor(FuncProblem<int, DummySearchSpace<int>> problem) : base(problem) { }

            private int invocations;

            public override IReadOnlyList<int> Mutate(IReadOnlyList<int> parents, IRandomNumberGenerator random)
            {
                var call = ++invocations;
                return parents.Select(parent => parent + call).ToArray();
            }
        }
    }

    private sealed record CounterState : SearchState
    {
        public required int Value { get; init; }
    }

    private sealed class CallbackTimeProvider(Func<long> timestamp) : TimeProvider
    {
        public override long GetTimestamp() => timestamp();
    }

    private sealed class AdvancingTimeProvider(TimeSpan step) : TimeProvider
    {
        private long timestamp;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp()
        {
            var current = timestamp;
            timestamp += step.Ticks;
            return current;
        }
    }
}

/// <summary>
/// Names the run's triple once for this file. Every mutator exercised here is authored over
/// <see cref="DummySearchSpace{T}"/>, so the triple is the same at every call site and repeating it per resolution
/// would only obscure what each test is actually asserting.
/// </summary>

/// <summary>
/// Names the run's triple once for this file. Every operator exercised here is authored over
/// <see cref="DummySearchSpace{T}"/>, so the triple is the same at every call site and repeating it per resolution
/// would only obscure what each test is actually asserting.
/// </summary>
file static class OperatorResolution
{
    private static readonly FuncProblem<int, DummySearchSpace<int>> Problem = FuncProblem.Create(static (int x) => x, DummySearchSpace<int>.Instance, SingleObjective.Minimize);
    extension(ResolutionScope scope)
    {
        public ICreationExecutor<int> ResolveCreator(IOperator<Creation, int> creator) =>
            scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(creator, Problem);

        public ICrossoverExecutor<int> ResolveCrossover(IOperator<Crossover, int> crossover) =>
            scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(crossover, Problem);

        public IMutationExecutor<int> ResolveMutator(IOperator<Mutation, int> mutator) =>
            scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(mutator, Problem);

        public ISelectionExecutor<int> ResolveSelector(IOperator<Selection, int> selector) =>
            scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(selector, Problem);

        public IReplacementExecutor<int> ResolveReplacer(IOperator<Replacement, int> replacer) =>
            scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(replacer, Problem);
    }

    extension(IOperator<Creation, int> creator)
    {
        public ICreationExecutor<int> CreateCreatorExecutor() =>
            ResolutionScope.Create().ResolveCreator(creator);
    }

    extension(IOperator<Crossover, int> crossover)
    {
        public ICrossoverExecutor<int> CreateCrossoverExecutor() =>
            ResolutionScope.Create().ResolveCrossover(crossover);
    }

    extension(IOperator<Mutation, int> mutator)
    {
        public IMutationExecutor<int> CreateMutatorExecutor() =>
            ResolutionScope.Create().ResolveMutator(mutator);
    }

    extension(IOperator<Selection, int> selector)
    {
        public ISelectionExecutor<int> CreateSelectorExecutor() =>
            ResolutionScope.Create().ResolveSelector(selector);
    }

    extension(IOperator<Replacement, int> replacer)
    {
        public IReplacementExecutor<int> CreateReplacerExecutor() =>
            ResolutionScope.Create().ResolveReplacer(replacer);
    }
}
