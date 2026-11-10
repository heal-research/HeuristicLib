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
    [InlineData(OperatorCountMetric.Invocations, 4, 5)]
    [InlineData(OperatorCountMetric.Candidates, 7, 8)]
    public void CountingMutator_NewExecutorsShareOnlyTheSuppliedCounter(OperatorCountMetric metric, int sharedCount, int independentCount)
    {
        var preparations = 0;
        var counter = new CountAccumulator();
        var observedCalls = new CountAccumulator();
        var leaf = new AdvancingMutator(() => preparations++);
        var source = leaf;
        var aspect = Aspect.Create(Pointcut.Reference(source), new CountingOperatorAdvice<Mutation, int>(counter, metric));
        var problem = CreateProblem();
        var random = RandomNumberGenerator.Create(1);
        var parent = ResolutionScope.Create();
        parent.AddAspect(aspect);
        var outer = parent.ResolveMutator(source);
        outer.Mutate([0, 0], random, problem.SearchSpace, problem).ShouldBe([1, 1]);

        var child = parent.CreateChildScope();
        child.AddAspect(Aspect.Create(Pointcut.Reference(leaf), new CountingOperatorAdvice(observedCalls, OperatorCountMetric.Invocations)));
        var inner = child.ResolveMutator(source);
        inner.ShouldNotBeSameAs(outer);
        inner.Mutate([0], random, problem.SearchSpace, problem).ShouldBe([1]);
        outer.Mutate([0, 0, 0], random, problem.SearchSpace, problem).ShouldBe([2, 2, 2]);
        inner.Mutate([0], random, problem.SearchSpace, problem).ShouldBe([2]);

        counter.CurrentCount.ShouldBe(sharedCount);
        observedCalls.CurrentCount.ShouldBe(2);
        preparations.ShouldBe(2);

        var independentScope = ResolutionScope.Create();
        independentScope.AddAspect(aspect);
        var independent = independentScope.ResolveMutator(source);
        independent.Mutate([0], random, problem.SearchSpace, problem).ShouldBe([1]);
        counter.CurrentCount.ShouldBe(independentCount);
        observedCalls.CurrentCount.ShouldBe(2);
        preparations.ShouldBe(3);
    }

    // A child scope builds new executors, which add to the one duration of the run. An independent run has its own.
    [Fact]
    public void TimedMutator_NewExecutorsShareOnlyTheDurationOfTheirRun()
    {
        var preparations = 0;
        var observedCalls = new CountAccumulator();
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var duration = new Duration(timeProvider);
        var leaf = new AdvancingMutator(() => preparations++);
        var source = leaf.Timed(duration);
        var problem = CreateProblem();
        var random = RandomNumberGenerator.Create(1);
        var parent = ResolutionScope.Create();
        var measured = parent.Results.Result(duration);
        var outer = parent.ResolveMutator(source);
        measured.CurrentDuration.ShouldBe(TimeSpan.Zero);
        outer.Mutate([0], random, problem.SearchSpace, problem).ShouldBe([1]);
        measured.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(3));

        var child = parent.CreateChildScope();
        child.AddAspect(Aspect.Create(Pointcut.Reference(leaf), new CountingOperatorAdvice(observedCalls, OperatorCountMetric.Invocations)));
        var inner = child.ResolveMutator(source);
        inner.ShouldNotBeSameAs(outer);
        measured.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(3));
        inner.Mutate([0], random, problem.SearchSpace, problem).ShouldBe([1]);
        measured.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(6));
        outer.Mutate([0], random, problem.SearchSpace, problem).ShouldBe([2]);
        inner.Mutate([0], random, problem.SearchSpace, problem).ShouldBe([2]);

        measured.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(12));
        observedCalls.CurrentCount.ShouldBe(2);
        preparations.ShouldBe(2);

        var independentScope = ResolutionScope.Create();
        var independent = independentScope.ResolveMutator(source);
        independent.Mutate([0], random, problem.SearchSpace, problem).ShouldBe([1]);
        measured.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(12));
        independentScope.Results.Result(duration).CurrentDuration.ShouldBe(TimeSpan.FromSeconds(3));
        observedCalls.CurrentCount.ShouldBe(2);
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
        var duration = new Duration(time);
        var scope = ResolutionScope.Create();
        var executor = scope.ResolveMutator(source.Timed(duration));
        var problem = CreateProblem();

        var failure = Record.Exception(() => executor.Mutate([1], RandomNumberGenerator.Create(1), problem.SearchSpace, problem));

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
        var duration = new Duration(time);
        var scope = ResolutionScope.Create();
        var executor = scope.ResolveMutator(source.Timed(duration));
        var problem = CreateProblem();

        Record.Exception(() => executor.Mutate([1], RandomNumberGenerator.Create(1), problem.SearchSpace, problem))
            .ShouldBeSameAs(failure);

        calls.ShouldBe(0);
        timestamps.ShouldBe(1);
        scope.Results.Result(duration).CurrentDuration.ShouldBe(TimeSpan.Zero);
    }

    [Fact]
    public void CountCreatorCalls_IncrementsOncePerCreateCall()
    {
        var counter = new CountAccumulator();
        var creator = new SequenceCreator();
        var aspect = Aspect.Create(Pointcut.Reference(creator), new CountingOperatorAdvice<Creation, int>(counter, OperatorCountMetric.Invocations));
        aspect.Single().Advice.ShouldBeOfType<CountingOperatorAdvice<Creation, int>>().Counter.ShouldBeSameAs(counter);
        aspect.Single().Advice.ShouldBeOfType<CountingOperatorAdvice<Creation, int>>().Metric.ShouldBe(OperatorCountMetric.Invocations);
        var scope = ResolutionScope.Create();
        scope.AddAspect(aspect);
        var executor = scope.ResolveCreator(creator);
        var problem = CreateProblem();

        executor.Create(3, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        executor.Create(1, RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        counter.CurrentCount.ShouldBe(2);
    }

    [Fact]
    public void CountCreatedCandidates_IncrementsByReturnedCandidateCount()
    {
        var counter = new CountAccumulator();
        var creator = new SequenceCreator();
        var aspect = Aspect.Create(Pointcut.Reference(creator), new CountingOperatorAdvice(counter, OperatorCountMetric.Candidates));
        var scope = ResolutionScope.Create();
        scope.AddAspect(aspect);
        var executor = scope.ResolveCreator(creator);
        var problem = CreateProblem();

        executor.Create(3, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        executor.Create(1, RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        counter.CurrentCount.ShouldBe(4);
    }

    [Fact]
    public void MeasureCreatorDuration_AddsElapsedCreatorExecutionDuration()
    {
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var duration = new Duration(timeProvider);
        var creator = new SequenceCreator();
        var scope = ResolutionScope.Create();
        var executor = scope.ResolveCreator(creator.Timed(duration));
        var problem = CreateProblem();

        executor.Create(3, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        executor.Create(1, RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        scope.Results.Result(duration).CurrentDuration.ShouldBe(TimeSpan.FromSeconds(6));
    }

    [Fact]
    public void CountCreatorCalls_DoesNotIncrementWhenCreationThrows()
    {
        var counter = new CountAccumulator();
        var source = new ThrowingCreator();
        var scope = ResolutionScope.Create();
        scope.AddAspect(Aspect.Create(Pointcut.Reference(source), new CountingOperatorAdvice(counter, OperatorCountMetric.Invocations)));
        var executor = scope.ResolveCreator(source);
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() =>
            executor.Create(1, RandomNumberGenerator.Create(1), problem.SearchSpace, problem));

        counter.CurrentCount.ShouldBe(0);
    }

    [Fact]
    public void MeasureCreatorDuration_RecordsElapsedDurationWhenCreationThrows()
    {
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var duration = new Duration(timeProvider);
        var source = new ThrowingCreator();
        var scope = ResolutionScope.Create();
        var executor = scope.ResolveCreator(source.Timed(duration));
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() =>
            executor.Create(1, RandomNumberGenerator.Create(1), problem.SearchSpace, problem));

        scope.Results.Result(duration).CurrentDuration.ShouldBe(TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void CountMutatorCalls_IncrementsOncePerMutateCall()
    {
        var counter = new CountAccumulator();
        var mutator = new AddOneMutator();
        var aspect = Aspect.Create(Pointcut.Reference(mutator), new CountingOperatorAdvice<Mutation, int>(counter, OperatorCountMetric.Invocations));
        aspect.Single().Advice.ShouldBeOfType<CountingOperatorAdvice<Mutation, int>>().Counter.ShouldBeSameAs(counter);
        aspect.Single().Advice.ShouldBeOfType<CountingOperatorAdvice<Mutation, int>>().Metric.ShouldBe(OperatorCountMetric.Invocations);
        var scope = ResolutionScope.Create();
        scope.AddAspect(aspect);
        var executor = scope.ResolveMutator(mutator);
        var problem = CreateProblem();

        executor.Mutate([1, 2, 3], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        executor.Mutate([4], RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        counter.CurrentCount.ShouldBe(2);
    }

    [Fact]
    public void CountMutatedCandidates_IncrementsByReturnedCandidateCount()
    {
        var counter = new CountAccumulator();
        var mutator = new AddOneMutator();
        var aspect = Aspect.Create(Pointcut.Reference(mutator), new CountingOperatorAdvice(counter, OperatorCountMetric.Candidates));
        var scope = ResolutionScope.Create();
        scope.AddAspect(aspect);
        var executor = scope.ResolveMutator(mutator);
        var problem = CreateProblem();

        executor.Mutate([1, 2, 3], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        executor.Mutate([4], RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        counter.CurrentCount.ShouldBe(4);
    }

    [Fact]
    public void CountMutatorCalls_DoesNotIncrementWhenMutationThrows()
    {
        var counter = new CountAccumulator();
        var source = new ThrowingMutator();
        var scope = ResolutionScope.Create();
        scope.AddAspect(Aspect.Create(Pointcut.Reference(source), new CountingOperatorAdvice(counter, OperatorCountMetric.Invocations)));
        var executor = scope.ResolveMutator(source);
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() =>
            executor.Mutate([1], RandomNumberGenerator.Create(1), problem.SearchSpace, problem));

        counter.CurrentCount.ShouldBe(0);
    }

    [Fact]
    public void MeasureMutatorDuration_AddsElapsedMutatorExecutionDuration()
    {
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var duration = new Duration(timeProvider);
        var mutator = new AddOneMutator();
        var scope = ResolutionScope.Create();
        var executor = scope.ResolveMutator(mutator.Timed(duration));
        var problem = CreateProblem();

        executor.Mutate([1, 2, 3], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        executor.Mutate([4], RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        scope.Results.Result(duration).CurrentDuration.ShouldBe(TimeSpan.FromSeconds(6));
    }

    [Fact]
    public void MeasureMutatorDuration_RecordsElapsedDurationWhenMutationThrows()
    {
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var duration = new Duration(timeProvider);
        var mutator = new ThrowingMutator();
        var scope = ResolutionScope.Create();
        var executor = scope.ResolveMutator(mutator.Timed(duration));
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() =>
            executor.Mutate([1], RandomNumberGenerator.Create(1), problem.SearchSpace, problem));

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
        var innerDuration = new Duration(timeProvider);
        var outerDuration = new Duration(timeProvider);
        var childMutator = new CallbackMutator((actualParents, actualRandom, actualSearchSpace, actualProblem) =>
        {
            calls++;
            actualParents.ShouldBeSameAs(parents);
            actualRandom.ShouldBeSameAs(random);
            actualSearchSpace.ShouldBeSameAs(problem.SearchSpace);
            actualProblem.ShouldBeSameAs(problem);
            return actualParents;
        });
        var scope = ResolutionScope.Create();
        var executor = scope.ResolveMutator(childMutator.Timed(innerDuration).Timed(outerDuration));

        var result = executor.Mutate(parents, random, problem.SearchSpace, problem);

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
        var innerDuration = new Duration(timeProvider);
        var outerDuration = new Duration(timeProvider);
        var childMutator = new CallbackMutator((_, _, _, _) =>
        {
            calls++;
            throw new InvalidOperationException();
        });
        var scope = ResolutionScope.Create();
        var executor = scope.ResolveMutator(childMutator.Timed(innerDuration).Timed(outerDuration));

        Should.Throw<InvalidOperationException>(() =>
            executor.Mutate([1], RandomNumberGenerator.Create(1), problem.SearchSpace, problem));

        calls.ShouldBe(1);
        scope.Results.Result(innerDuration).CurrentDuration.ShouldBe(TimeSpan.FromSeconds(3));
        scope.Results.Result(outerDuration).CurrentDuration.ShouldBe(TimeSpan.FromSeconds(9));
    }

    [Fact]
    public void CountCrossoverCalls_IncrementsOncePerCrossCall()
    {
        var counter = new CountAccumulator();
        var crossover = new SumParentsCrossover();
        var aspect = Aspect.Create(Pointcut.Reference(crossover), new CountingOperatorAdvice<Crossover, int>(counter, OperatorCountMetric.Invocations));
        aspect.Single().Advice.ShouldBeOfType<CountingOperatorAdvice<Crossover, int>>().Counter.ShouldBeSameAs(counter);
        aspect.Single().Advice.ShouldBeOfType<CountingOperatorAdvice<Crossover, int>>().Metric.ShouldBe(OperatorCountMetric.Invocations);
        var scope = ResolutionScope.Create();
        scope.AddAspect(aspect);
        var executor = scope.ResolveCrossover(crossover);
        var problem = CreateProblem();

        executor.Cross(
            [Parents.From(1, 10), Parents.From(2, 20), Parents.From(3, 30)],
            RandomNumberGenerator.Create(1),
            problem.SearchSpace,
            problem);
        executor.Cross(
            [Parents.From(4, 40)],
            RandomNumberGenerator.Create(2),
            problem.SearchSpace,
            problem);

        counter.CurrentCount.ShouldBe(2);
    }

    [Fact]
    public void CountCrossedCandidates_IncrementsByReturnedCandidateCount()
    {
        var counter = new CountAccumulator();
        var crossover = new SumParentsCrossover();
        var aspect = Aspect.Create(Pointcut.Reference(crossover), new CountingOperatorAdvice(counter, OperatorCountMetric.Candidates));
        var scope = ResolutionScope.Create();
        scope.AddAspect(aspect);
        var executor = scope.ResolveCrossover(crossover);
        var problem = CreateProblem();

        executor.Cross(
            [Parents.From(1, 10), Parents.From(2, 20), Parents.From(3, 30)],
            RandomNumberGenerator.Create(1),
            problem.SearchSpace,
            problem);
        executor.Cross(
            [Parents.From(4, 40)],
            RandomNumberGenerator.Create(2),
            problem.SearchSpace,
            problem);

        counter.CurrentCount.ShouldBe(4);
    }

    [Fact]
    public void MeasureCrossoverDuration_AddsElapsedCrossoverExecutionDuration()
    {
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var duration = new Duration(timeProvider);
        var crossover = new SumParentsCrossover();
        var scope = ResolutionScope.Create();
        var executor = scope.ResolveCrossover(crossover.Timed(duration));
        var problem = CreateProblem();

        executor.Cross(
            [Parents.From(1, 10), Parents.From(2, 20), Parents.From(3, 30)],
            RandomNumberGenerator.Create(1),
            problem.SearchSpace,
            problem);
        executor.Cross(
            [Parents.From(4, 40)],
            RandomNumberGenerator.Create(2),
            problem.SearchSpace,
            problem);

        scope.Results.Result(duration).CurrentDuration.ShouldBe(TimeSpan.FromSeconds(6));
    }

    [Fact]
    public void CountCrossoverCalls_DoesNotIncrementWhenCrossoverThrows()
    {
        var counter = new CountAccumulator();
        var source = new ThrowingCrossover();
        var scope = ResolutionScope.Create();
        scope.AddAspect(Aspect.Create(Pointcut.Reference(source), new CountingOperatorAdvice(counter, OperatorCountMetric.Invocations)));
        var executor = scope.ResolveCrossover(source);
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() =>
            executor.Cross([Parents.From(1, 10)], RandomNumberGenerator.Create(1), problem.SearchSpace, problem));

        counter.CurrentCount.ShouldBe(0);
    }

    [Fact]
    public void MeasureCrossoverDuration_RecordsElapsedDurationWhenCrossoverThrows()
    {
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var duration = new Duration(timeProvider);
        var source = new ThrowingCrossover();
        var scope = ResolutionScope.Create();
        var executor = scope.ResolveCrossover(source.Timed(duration));
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() =>
            executor.Cross([Parents.From(1, 10)], RandomNumberGenerator.Create(1), problem.SearchSpace, problem));

        scope.Results.Result(duration).CurrentDuration.ShouldBe(TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void CountSelectorCalls_IncrementsOncePerSelectCall()
    {
        var counter = new CountAccumulator();
        var selector = new FirstCandidatesSelector();
        var aspect = Aspect.Create(Pointcut.Reference(selector), new CountingOperatorAdvice<Selection, int>(counter, OperatorCountMetric.Invocations));
        aspect.Single().Advice.ShouldBeOfType<CountingOperatorAdvice<Selection, int>>().Counter.ShouldBeSameAs(counter);
        aspect.Single().Advice.ShouldBeOfType<CountingOperatorAdvice<Selection, int>>().Metric.ShouldBe(OperatorCountMetric.Invocations);
        var scope = ResolutionScope.Create();
        scope.AddAspect(aspect);
        var executor = scope.ResolveSelector(selector);
        var problem = CreateProblem();

        executor.Select(CreateEvaluatedCandidates([1, 2, 3]), problem.Objective, 2, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        executor.Select(CreateEvaluatedCandidates([4]), problem.Objective, 1, RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        counter.CurrentCount.ShouldBe(2);
    }

    [Fact]
    public void CountSelectedCandidates_IncrementsByReturnedCandidateCount()
    {
        var counter = new CountAccumulator();
        var selector = new FirstCandidatesSelector();
        var aspect = Aspect.Create(Pointcut.Reference(selector), new CountingOperatorAdvice(counter, OperatorCountMetric.Candidates));
        var scope = ResolutionScope.Create();
        scope.AddAspect(aspect);
        var executor = scope.ResolveSelector(selector);
        var problem = CreateProblem();

        executor.Select(CreateEvaluatedCandidates([1, 2, 3]), problem.Objective, 2, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        executor.Select(CreateEvaluatedCandidates([4]), problem.Objective, 1, RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        counter.CurrentCount.ShouldBe(3);
    }

    [Fact]
    public void CountSelectorCalls_DoesNotIncrementWhenSelectionThrows()
    {
        var counter = new CountAccumulator();
        var source = new ThrowingSelector();
        var scope = ResolutionScope.Create();
        scope.AddAspect(Aspect.Create(Pointcut.Reference(source), new CountingOperatorAdvice(counter, OperatorCountMetric.Invocations)));
        var executor = scope.ResolveSelector(source);
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() =>
            executor.Select(CreateEvaluatedCandidates([1]), problem.Objective, 1, RandomNumberGenerator.Create(1), problem.SearchSpace, problem));

        counter.CurrentCount.ShouldBe(0);
    }

    [Fact]
    public void MeasureSelectorDuration_AddsElapsedSelectorExecutionDuration()
    {
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var duration = new Duration(timeProvider);
        var selector = new FirstCandidatesSelector();
        var scope = ResolutionScope.Create();
        var executor = scope.ResolveSelector(selector.Timed(duration));
        var problem = CreateProblem();

        executor.Select(CreateEvaluatedCandidates([1, 2, 3]), problem.Objective, 2, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        executor.Select(CreateEvaluatedCandidates([4]), problem.Objective, 1, RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        scope.Results.Result(duration).CurrentDuration.ShouldBe(TimeSpan.FromSeconds(6));
    }

    [Fact]
    public void MeasureSelectorDuration_RecordsElapsedDurationWhenSelectionThrows()
    {
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var duration = new Duration(timeProvider);
        var selector = new ThrowingSelector();
        var scope = ResolutionScope.Create();
        var executor = scope.ResolveSelector(selector.Timed(duration));
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() =>
            executor.Select(CreateEvaluatedCandidates([1]), problem.Objective, 1, RandomNumberGenerator.Create(1), problem.SearchSpace, problem));

        scope.Results.Result(duration).CurrentDuration.ShouldBe(TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void CountReplacerCalls_IncrementsOncePerReplaceCall()
    {
        var counter = new CountAccumulator();
        var replacer = new FirstReplacementCandidatesReplacer();
        var aspect = Aspect.Create(Pointcut.Reference(replacer), new CountingOperatorAdvice<Replacement, int>(counter, OperatorCountMetric.Invocations));
        aspect.Single().Advice.ShouldBeOfType<CountingOperatorAdvice<Replacement, int>>().Counter.ShouldBeSameAs(counter);
        aspect.Single().Advice.ShouldBeOfType<CountingOperatorAdvice<Replacement, int>>().Metric.ShouldBe(OperatorCountMetric.Invocations);
        var scope = ResolutionScope.Create();
        scope.AddAspect(aspect);
        var executor = scope.ResolveReplacer(replacer);
        var problem = CreateProblem();

        executor.Replace(
            CreateEvaluatedCandidates([1, 2, 3]),
            CreateEvaluatedCandidates([10, 20]),
            problem.Objective,
            2,
            RandomNumberGenerator.Create(1),
            problem.SearchSpace,
            problem);
        executor.Replace(
            CreateEvaluatedCandidates([4]),
            CreateEvaluatedCandidates([40]),
            problem.Objective,
            1,
            RandomNumberGenerator.Create(2),
            problem.SearchSpace,
            problem);

        counter.CurrentCount.ShouldBe(2);
    }

    [Fact]
    public void CountReplacementCandidates_IncrementsByReturnedCandidateCount()
    {
        var counter = new CountAccumulator();
        var replacer = new FirstReplacementCandidatesReplacer();
        var aspect = Aspect.Create(Pointcut.Reference(replacer), new CountingOperatorAdvice(counter, OperatorCountMetric.Candidates));
        var scope = ResolutionScope.Create();
        scope.AddAspect(aspect);
        var executor = scope.ResolveReplacer(replacer);
        var problem = CreateProblem();

        executor.Replace(
            CreateEvaluatedCandidates([1, 2, 3]),
            CreateEvaluatedCandidates([10, 20]),
            problem.Objective,
            2,
            RandomNumberGenerator.Create(1),
            problem.SearchSpace,
            problem);
        executor.Replace(
            CreateEvaluatedCandidates([4]),
            CreateEvaluatedCandidates([40]),
            problem.Objective,
            1,
            RandomNumberGenerator.Create(2),
            problem.SearchSpace,
            problem);

        counter.CurrentCount.ShouldBe(3);
    }

    [Fact]
    public void CountReplacerCalls_DoesNotCountFailedCall()
    {
        var counter = new CountAccumulator();
        var source = new ThrowingReplacer();
        var scope = ResolutionScope.Create();
        scope.AddAspect(Aspect.Create(Pointcut.Reference(source), new CountingOperatorAdvice(counter, OperatorCountMetric.Invocations)));
        var executor = scope.ResolveReplacer(source);
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() => executor.Replace(
            CreateEvaluatedCandidates([1]),
            CreateEvaluatedCandidates([2]),
            problem.Objective,
            1,
            RandomNumberGenerator.Create(1),
            problem.SearchSpace,
            problem));

        counter.CurrentCount.ShouldBe(0);
    }

    [Fact]
    public void MeasureReplacerDuration_AddsElapsedReplacerExecutionDuration()
    {
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var duration = new Duration(timeProvider);
        var replacer = new FirstReplacementCandidatesReplacer();
        var scope = ResolutionScope.Create();
        var executor = scope.ResolveReplacer(replacer.Timed(duration));
        var problem = CreateProblem();

        executor.Replace(
            CreateEvaluatedCandidates([1, 2, 3]),
            CreateEvaluatedCandidates([10, 20]),
            problem.Objective,
            2,
            RandomNumberGenerator.Create(1),
            problem.SearchSpace,
            problem);
        executor.Replace(
            CreateEvaluatedCandidates([4]),
            CreateEvaluatedCandidates([40]),
            problem.Objective,
            1,
            RandomNumberGenerator.Create(2),
            problem.SearchSpace,
            problem);

        scope.Results.Result(duration).CurrentDuration.ShouldBe(TimeSpan.FromSeconds(6));
    }

    [Fact]
    public void MeasureReplacerDuration_RecordsFailedCall()
    {
        var duration = new Duration(new AdvancingTimeProvider(TimeSpan.FromSeconds(3)));
        var source = new ThrowingReplacer();
        var scope = ResolutionScope.Create();
        var executor = scope.ResolveReplacer(source.Timed(duration));
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() => executor.Replace(
            CreateEvaluatedCandidates([1]),
            CreateEvaluatedCandidates([2]),
            problem.Objective,
            1,
            RandomNumberGenerator.Create(1),
            problem.SearchSpace,
            problem));

        scope.Results.Result(duration).CurrentDuration.ShouldBe(TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void CountInterceptorCalls_IncrementsOncePerTransformCall()
    {
        var counter = new CountAccumulator();
        var interceptor = new AddOneInterceptor();
        var aspect = Aspect.Create(Pointcut.Reference(interceptor), new CountingOperatorAdvice<Interception<CounterState>, int>(counter, OperatorCountMetric.Invocations));
        aspect.Single().Advice.ShouldBeOfType<CountingOperatorAdvice<Interception<CounterState>, int>>().Counter.ShouldBeSameAs(counter);
        var scope = ResolutionScope.Create();
        scope.AddAspect(aspect);
        var executor = scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, CounterState>(interceptor);
        var problem = CreateProblem();

        executor.Transform(new CounterState { Value = 1 }, previousState: null, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        executor.Transform(new CounterState { Value = 2 }, new CounterState { Value = 1 }, RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        counter.CurrentCount.ShouldBe(2);
    }

    [Fact]
    public void MeasureInterceptorDuration_AddsElapsedInterceptorExecutionDuration()
    {
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var duration = new Duration(timeProvider);
        var interceptor = new AddOneInterceptor();
        var scope = ResolutionScope.Create();
        var executor = scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, CounterState>(interceptor.Timed(duration));
        var problem = CreateProblem();

        executor.Transform(new CounterState { Value = 1 }, previousState: null, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        executor.Transform(new CounterState { Value = 2 }, new CounterState { Value = 1 }, RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        scope.Results.Result(duration).CurrentDuration.ShouldBe(TimeSpan.FromSeconds(6));
    }

    [Fact]
    public void CountInterceptorCalls_DoesNotCountFailedCall()
    {
        var counter = new CountAccumulator();
        var source = new ThrowingInterceptor();
        var scope = ResolutionScope.Create();
        scope.AddAspect(Aspect.Create(Pointcut.Reference(source), new CountingOperatorAdvice(counter, OperatorCountMetric.Invocations)));
        var executor = scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, CounterState>(source);
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() => executor.Transform(new CounterState { Value = 1 }, null, RandomNumberGenerator.Create(1), problem.SearchSpace, problem));

        counter.CurrentCount.ShouldBe(0);
    }

    [Fact]
    public void MeasureInterceptorDuration_RecordsFailedCall()
    {
        var duration = new Duration(new AdvancingTimeProvider(TimeSpan.FromSeconds(3)));
        var source = new ThrowingInterceptor();
        var scope = ResolutionScope.Create();
        var executor = scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, CounterState>(source.Timed(duration));
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() => executor.Transform(new CounterState { Value = 1 }, null, RandomNumberGenerator.Create(1), problem.SearchSpace, problem));

        scope.Results.Result(duration).CurrentDuration.ShouldBe(TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void CountTerminatorCalls_IncrementsOncePerTerminalStateCheck()
    {
        var counter = new CountAccumulator();
        var terminator = new NeverTerminalStateTerminator();
        var aspect = Aspect.Create(Pointcut.Reference(terminator), new CountingOperatorAdvice<Termination, int>(counter, OperatorCountMetric.Invocations));
        aspect.Single().Advice.ShouldBeOfType<CountingOperatorAdvice<Termination, int>>().Counter.ShouldBeSameAs(counter);
        var scope = ResolutionScope.Create();
        scope.AddAspect(aspect);
        var executor = scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, CounterState>(terminator);
        var problem = CreateProblem();

        executor.IsTerminalState(new CounterState { Value = 1 }, RandomNumberGenerator.Create(0), problem.SearchSpace, problem);
        executor.IsTerminalState(new CounterState { Value = 2 }, RandomNumberGenerator.Create(0), problem.SearchSpace, problem);

        counter.CurrentCount.ShouldBe(2);
    }

    [Fact]
    public void MeasureTerminatorDuration_AddsElapsedTerminatorExecutionDuration()
    {
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var duration = new Duration(timeProvider);
        var terminator = new NeverTerminalStateTerminator();
        var scope = ResolutionScope.Create();
        var executor = scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, CounterState>(terminator.Timed(duration));
        var problem = CreateProblem();

        executor.IsTerminalState(new CounterState { Value = 1 }, RandomNumberGenerator.Create(0), problem.SearchSpace, problem);
        executor.IsTerminalState(new CounterState { Value = 2 }, RandomNumberGenerator.Create(0), problem.SearchSpace, problem);

        scope.Results.Result(duration).CurrentDuration.ShouldBe(TimeSpan.FromSeconds(6));
    }

    [Fact]
    public void CountTerminatorCalls_DoesNotCountFailedCall()
    {
        var counter = new CountAccumulator();
        var source = new ThrowingTerminator();
        var scope = ResolutionScope.Create();
        scope.AddAspect(Aspect.Create(Pointcut.Reference(source), new CountingOperatorAdvice(counter, OperatorCountMetric.Invocations)));
        var executor = scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, CounterState>(source);
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() => executor.IsTerminalState(new CounterState { Value = 1 }, RandomNumberGenerator.Create(0), problem.SearchSpace, problem));

        counter.CurrentCount.ShouldBe(0);
    }

    [Fact]
    public void MeasureTerminatorDuration_RecordsFailedCall()
    {
        var duration = new Duration(new AdvancingTimeProvider(TimeSpan.FromSeconds(3)));
        var source = new ThrowingTerminator();
        var scope = ResolutionScope.Create();
        var executor = scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, CounterState>(source.Timed(duration));
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() => executor.IsTerminalState(new CounterState { Value = 1 }, RandomNumberGenerator.Create(0), problem.SearchSpace, problem));

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

    private sealed class CallbackMutator(MutateCallback callback)
        : IOperator<Mutation, int>
    {
        IExecutor IOperator<int>.CreateExecutor<TSearchSpace, TProblem, TSearchState>(
            ResolutionScope<int, TSearchSpace, TProblem, TSearchState> scope) => CreateBoundExecutor();

        private IMutationExecutor<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> CreateBoundExecutor() =>
            new Executor(callback);

        private sealed class Executor(MutateCallback callback)
            : IMutationExecutor<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>
        {
            public IReadOnlyList<int> Mutate(
                IReadOnlyList<int> parents,
                IRandomNumberGenerator random,
                DummySearchSpace<int> searchSpace,
                FuncProblem<int, DummySearchSpace<int>> problem) =>
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

        protected override IMutationExecutor<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> CreateExecutor(ResolutionScope scope)
        {
            Prepare();
            return new Executor();
        }

        private sealed class Executor
            : MutationExecutor<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>
        {
            private int invocations;

            public override IReadOnlyList<int> Mutate(IReadOnlyList<int> parents, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace, FuncProblem<int, DummySearchSpace<int>> problem)
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
    extension(ResolutionScope scope)
    {
        public ICreationExecutor<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> ResolveCreator(IOperator<Creation, int> creator) =>
            scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(creator);

        public ICrossoverExecutor<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> ResolveCrossover(IOperator<Crossover, int> crossover) =>
            scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(crossover);

        public IMutationExecutor<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> ResolveMutator(IOperator<Mutation, int> mutator) =>
            scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(mutator);

        public ISelectionExecutor<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> ResolveSelector(IOperator<Selection, int> selector) =>
            scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(selector);

        public IReplacementExecutor<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> ResolveReplacer(IOperator<Replacement, int> replacer) =>
            scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(replacer);
    }

    extension(IOperator<Creation, int> creator)
    {
        public ICreationExecutor<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> CreateCreatorExecutor() =>
            ResolutionScope.Create().ResolveCreator(creator);
    }

    extension(IOperator<Crossover, int> crossover)
    {
        public ICrossoverExecutor<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> CreateCrossoverExecutor() =>
            ResolutionScope.Create().ResolveCrossover(crossover);
    }

    extension(IOperator<Mutation, int> mutator)
    {
        public IMutationExecutor<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> CreateMutatorExecutor() =>
            ResolutionScope.Create().ResolveMutator(mutator);
    }

    extension(IOperator<Selection, int> selector)
    {
        public ISelectionExecutor<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> CreateSelectorExecutor() =>
            ResolutionScope.Create().ResolveSelector(selector);
    }

    extension(IOperator<Replacement, int> replacer)
    {
        public IReplacementExecutor<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> CreateReplacerExecutor() =>
            ResolutionScope.Create().ResolveReplacer(replacer);
    }
}
