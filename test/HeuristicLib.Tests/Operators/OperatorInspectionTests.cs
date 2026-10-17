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
    [InlineData(OperatorCountMetric.Calls, 4, 5)]
    [InlineData(OperatorCountMetric.Candidates, 7, 8)]
    public void CountingMutator_RebindingPreservesChildStateAndTheSuppliedCounter(OperatorCountMetric metric, int sharedCount, int independentCount)
    {
        var preparations = 0;
        var counter = new CountAccumulator();
        var observedCalls = new CountAccumulator();
        var leaf = new AdvancingMutator(() => preparations++);
        var source = leaf;
        var aspect = Aspect.Create(Pointcut.Reference(source), new CountingOperatorAdvice<Mutation, int>(counter, metric));
        var problem = CreateProblem();
        var random = RandomNumberGenerator.Create(1);
        var parent = ResolutionScope.Create(aspect);
        var outer = parent.ResolveMutator(source);
        outer.Mutate([0, 0], random, problem.SearchSpace, problem).ShouldBe([1, 1]);

        var child = parent.CreateChildScope(Aspect.CountCalls(leaf, observedCalls));
        var inner = child.ResolveMutator(source);
        inner.ShouldNotBeSameAs(outer);
        inner.Mutate([0], random, problem.SearchSpace, problem).ShouldBe([2]);
        outer.Mutate([0, 0, 0], random, problem.SearchSpace, problem).ShouldBe([3, 3, 3]);
        inner.Mutate([0], random, problem.SearchSpace, problem).ShouldBe([4]);

        counter.CurrentCount.ShouldBe(sharedCount);
        observedCalls.CurrentCount.ShouldBe(2);
        preparations.ShouldBe(1);

        var independent = ResolutionScope.Create(aspect).ResolveMutator(source);
        independent.Mutate([0], random, problem.SearchSpace, problem).ShouldBe([1]);
        counter.CurrentCount.ShouldBe(independentCount);
        observedCalls.CurrentCount.ShouldBe(2);
        preparations.ShouldBe(2);
    }

    [Fact]
    public void DurationMeasuringMutator_RebindingPreservesChildStateAndTheSuppliedDuration()
    {
        var preparations = 0;
        var duration = new DurationAccumulator();
        var observedCalls = new CountAccumulator();
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var leaf = new AdvancingMutator(() => preparations++);
        var source = leaf;
        var aspect = Aspect.MeasureDuration(source, duration, timeProvider);
        var problem = CreateProblem();
        var random = RandomNumberGenerator.Create(1);
        var parent = ResolutionScope.Create(aspect);
        var outer = parent.ResolveMutator(source);
        duration.CurrentDuration.ShouldBe(TimeSpan.Zero);
        outer.Mutate([0], random, problem.SearchSpace, problem).ShouldBe([1]);
        duration.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(3));

        var child = parent.CreateChildScope(Aspect.CountCalls(leaf, observedCalls));
        var inner = child.ResolveMutator(source);
        inner.ShouldNotBeSameAs(outer);
        duration.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(3));
        inner.Mutate([0], random, problem.SearchSpace, problem).ShouldBe([2]);
        duration.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(6));
        outer.Mutate([0], random, problem.SearchSpace, problem).ShouldBe([3]);
        inner.Mutate([0], random, problem.SearchSpace, problem).ShouldBe([4]);

        duration.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(12));
        observedCalls.CurrentCount.ShouldBe(2);
        preparations.ShouldBe(1);

        var independent = ResolutionScope.Create(aspect).ResolveMutator(source);
        duration.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(12));
        independent.Mutate([0], random, problem.SearchSpace, problem).ShouldBe([1]);
        duration.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(15));
        observedCalls.CurrentCount.ShouldBe(2);
        preparations.ShouldBe(2);
    }

    [Fact]
    public void CountCreatorCalls_IncrementsOncePerCreateCall()
    {
        var counter = new CountAccumulator();
        var creator = new SequenceCreator();
        var aspect = Aspect.CountCalls(creator, counter);
        aspect.Advices.Single().Advice.ShouldBeOfType<CountingCreatorAdvice<int>>().Counter.ShouldBeSameAs(counter);
        aspect.Advices.Single().Advice.ShouldBeOfType<CountingCreatorAdvice<int>>().Metric.ShouldBe(OperatorCountMetric.Calls);
        var executor = ResolutionScope.Create(aspect).ResolveCreator(creator);
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
        var aspect = Aspect.CountCandidates(creator, counter);
        var executor = ResolutionScope.Create(aspect).ResolveCreator(creator);
        var problem = CreateProblem();

        executor.Create(3, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        executor.Create(1, RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        counter.CurrentCount.ShouldBe(4);
    }

    [Fact]
    public void MeasureCreatorDuration_AddsElapsedCreatorExecutionDuration()
    {
        var duration = new DurationAccumulator();
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var creator = new SequenceCreator();
        var aspect = Aspect.MeasureDuration(creator, duration, timeProvider);
        aspect.Advices.Single().Advice.ShouldBeOfType<DurationMeasuringCreatorAdvice<int>>().Duration.ShouldBeSameAs(duration);
        aspect.Advices.Single().Advice.ShouldBeOfType<DurationMeasuringCreatorAdvice<int>>().TimeProvider.ShouldBeSameAs(timeProvider);
        var executor = ResolutionScope.Create(aspect).ResolveCreator(creator);
        var problem = CreateProblem();

        executor.Create(3, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        executor.Create(1, RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        duration.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(6));
    }

    [Fact]
    public void CountCreatorCalls_DoesNotIncrementWhenCreationThrows()
    {
        var counter = new CountAccumulator();
        var source = new ThrowingCreator();
        var executor = ResolutionScope.Create(Aspect.CountCalls(source, counter)).ResolveCreator(source);
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() =>
            executor.Create(1, RandomNumberGenerator.Create(1), problem.SearchSpace, problem));

        counter.CurrentCount.ShouldBe(0);
    }

    [Fact]
    public void MeasureCreatorDuration_RecordsElapsedDurationWhenCreationThrows()
    {
        var duration = new DurationAccumulator();
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var source = new ThrowingCreator();
        var executor = ResolutionScope.Create(Aspect.MeasureDuration(source, duration, timeProvider)).ResolveCreator(source);
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() =>
            executor.Create(1, RandomNumberGenerator.Create(1), problem.SearchSpace, problem));

        duration.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void CountMutatorCalls_IncrementsOncePerMutateCall()
    {
        var counter = new CountAccumulator();
        var mutator = new AddOneMutator();
        var aspect = Aspect.CountCalls(mutator, counter);
        aspect.Advices.Single().Advice.ShouldBeOfType<CountingOperatorAdvice<Mutation, int>>().Counter.ShouldBeSameAs(counter);
        aspect.Advices.Single().Advice.ShouldBeOfType<CountingOperatorAdvice<Mutation, int>>().Metric.ShouldBe(OperatorCountMetric.Calls);
        var executor = ResolutionScope.Create(aspect).ResolveMutator(mutator);
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
        var aspect = Aspect.CountCandidates(mutator, counter);
        var executor = ResolutionScope.Create(aspect).ResolveMutator(mutator);
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
        var executor = ResolutionScope.Create(Aspect.CountCalls(source, counter)).ResolveMutator(source);
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() =>
            executor.Mutate([1], RandomNumberGenerator.Create(1), problem.SearchSpace, problem));

        counter.CurrentCount.ShouldBe(0);
    }

    [Fact]
    public void MeasureMutatorDuration_AddsElapsedMutatorExecutionDuration()
    {
        var duration = new DurationAccumulator();
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var mutator = new AddOneMutator();
        var aspect = Aspect.MeasureDuration(mutator, duration, timeProvider);
        aspect.Advices.Single().Advice.ShouldBeOfType<DurationMeasuringOperatorAdvice<Mutation, int>>().Duration.ShouldBeSameAs(duration);
        aspect.Advices.Single().Advice.ShouldBeOfType<DurationMeasuringOperatorAdvice<Mutation, int>>().TimeProvider.ShouldBeSameAs(timeProvider);
        var executor = ResolutionScope.Create(aspect).ResolveMutator(mutator);
        var problem = CreateProblem();

        executor.Mutate([1, 2, 3], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        executor.Mutate([4], RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        duration.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(6));
    }

    [Fact]
    public void MeasureMutatorDuration_RecordsElapsedDurationWhenMutationThrows()
    {
        var duration = new DurationAccumulator();
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var mutator = new ThrowingMutator();
        var aspect = Aspect.MeasureDuration(mutator, duration, timeProvider);
        var executor = ResolutionScope.Create(aspect).ResolveMutator(mutator);
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() =>
            executor.Mutate([1], RandomNumberGenerator.Create(1), problem.SearchSpace, problem));

        duration.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void NestedDurationMeasuringMutators_ForwardTheSameInvocationDataAndInvokeTheChildMutatorOnce()
    {
        var calls = 0;
        IReadOnlyList<int> parents = [1, 2, 3];
        var random = RandomNumberGenerator.Create(1);
        var problem = CreateProblem();
        var innerDuration = new DurationAccumulator();
        var outerDuration = new DurationAccumulator();
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var childMutator = new CallbackMutator((actualParents, actualRandom, actualSearchSpace, actualProblem) =>
        {
            calls++;
            actualParents.ShouldBeSameAs(parents);
            actualRandom.ShouldBeSameAs(random);
            actualSearchSpace.ShouldBeSameAs(problem.SearchSpace);
            actualProblem.ShouldBeSameAs(problem);
            return actualParents;
        });
        var executor = ResolutionScope.Create(Aspect.MeasureDuration(childMutator, innerDuration, timeProvider),
            Aspect.MeasureDuration(childMutator, outerDuration, timeProvider)).ResolveMutator(childMutator);

        var result = executor.Mutate(parents, random, problem.SearchSpace, problem);

        result.ShouldBeSameAs(parents);
        calls.ShouldBe(1);
        innerDuration.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(3));
        outerDuration.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(9));
    }

    [Fact]
    public void NestedDurationMeasuringMutators_RecordEveryDurationWhenTheChildMutatorThrows()
    {
        var calls = 0;
        var problem = CreateProblem();
        var innerDuration = new DurationAccumulator();
        var outerDuration = new DurationAccumulator();
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var childMutator = new CallbackMutator((_, _, _, _) =>
        {
            calls++;
            throw new InvalidOperationException();
        });
        var executor = ResolutionScope.Create(Aspect.MeasureDuration(childMutator, innerDuration, timeProvider),
            Aspect.MeasureDuration(childMutator, outerDuration, timeProvider)).ResolveMutator(childMutator);

        Should.Throw<InvalidOperationException>(() =>
            executor.Mutate([1], RandomNumberGenerator.Create(1), problem.SearchSpace, problem));

        calls.ShouldBe(1);
        innerDuration.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(3));
        outerDuration.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(9));
    }

    [Fact]
    public void CountCrossoverCalls_IncrementsOncePerCrossCall()
    {
        var counter = new CountAccumulator();
        var crossover = new SumParentsCrossover();
        var aspect = Aspect.CountCalls(crossover, counter);
        aspect.Advices.Single().Advice.ShouldBeOfType<CountingCrossoverAdvice<int>>().Counter.ShouldBeSameAs(counter);
        aspect.Advices.Single().Advice.ShouldBeOfType<CountingCrossoverAdvice<int>>().Metric.ShouldBe(OperatorCountMetric.Calls);
        var executor = ResolutionScope.Create(aspect).ResolveCrossover(crossover);
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
        var aspect = Aspect.CountCandidates(crossover, counter);
        var executor = ResolutionScope.Create(aspect).ResolveCrossover(crossover);
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
        var duration = new DurationAccumulator();
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var crossover = new SumParentsCrossover();
        var aspect = Aspect.MeasureDuration(crossover, duration, timeProvider);
        aspect.Advices.Single().Advice.ShouldBeOfType<DurationMeasuringCrossoverAdvice<int>>().Duration.ShouldBeSameAs(duration);
        aspect.Advices.Single().Advice.ShouldBeOfType<DurationMeasuringCrossoverAdvice<int>>().TimeProvider.ShouldBeSameAs(timeProvider);
        var executor = ResolutionScope.Create(aspect).ResolveCrossover(crossover);
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

        duration.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(6));
    }

    [Fact]
    public void CountCrossoverCalls_DoesNotIncrementWhenCrossoverThrows()
    {
        var counter = new CountAccumulator();
        var source = new ThrowingCrossover();
        var executor = ResolutionScope.Create(Aspect.CountCalls(source, counter)).ResolveCrossover(source);
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() =>
            executor.Cross([Parents.From(1, 10)], RandomNumberGenerator.Create(1), problem.SearchSpace, problem));

        counter.CurrentCount.ShouldBe(0);
    }

    [Fact]
    public void MeasureCrossoverDuration_RecordsElapsedDurationWhenCrossoverThrows()
    {
        var duration = new DurationAccumulator();
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var source = new ThrowingCrossover();
        var executor = ResolutionScope.Create(Aspect.MeasureDuration(source, duration, timeProvider)).ResolveCrossover(source);
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() =>
            executor.Cross([Parents.From(1, 10)], RandomNumberGenerator.Create(1), problem.SearchSpace, problem));

        duration.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void CountSelectorCalls_IncrementsOncePerSelectCall()
    {
        var counter = new CountAccumulator();
        var selector = new FirstCandidatesSelector();
        var aspect = Aspect.CountCalls(selector, counter);
        aspect.Advices.Single().Advice.ShouldBeOfType<CountingSelectorAdvice<int>>().Counter.ShouldBeSameAs(counter);
        aspect.Advices.Single().Advice.ShouldBeOfType<CountingSelectorAdvice<int>>().Metric.ShouldBe(OperatorCountMetric.Calls);
        var executor = ResolutionScope.Create(aspect).ResolveSelector(selector);
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
        var aspect = Aspect.CountCandidates(selector, counter);
        var executor = ResolutionScope.Create(aspect).ResolveSelector(selector);
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
        var executor = ResolutionScope.Create(Aspect.CountCalls(source, counter)).ResolveSelector(source);
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() =>
            executor.Select(CreateEvaluatedCandidates([1]), problem.Objective, 1, RandomNumberGenerator.Create(1), problem.SearchSpace, problem));

        counter.CurrentCount.ShouldBe(0);
    }

    [Fact]
    public void MeasureSelectorDuration_AddsElapsedSelectorExecutionDuration()
    {
        var duration = new DurationAccumulator();
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var selector = new FirstCandidatesSelector();
        var aspect = Aspect.MeasureDuration(selector, duration, timeProvider);
        aspect.Advices.Single().Advice.ShouldBeOfType<DurationMeasuringSelectorAdvice<int>>().Duration.ShouldBeSameAs(duration);
        aspect.Advices.Single().Advice.ShouldBeOfType<DurationMeasuringSelectorAdvice<int>>().TimeProvider.ShouldBeSameAs(timeProvider);
        var executor = ResolutionScope.Create(aspect).ResolveSelector(selector);
        var problem = CreateProblem();

        executor.Select(CreateEvaluatedCandidates([1, 2, 3]), problem.Objective, 2, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        executor.Select(CreateEvaluatedCandidates([4]), problem.Objective, 1, RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        duration.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(6));
    }

    [Fact]
    public void MeasureSelectorDuration_RecordsElapsedDurationWhenSelectionThrows()
    {
        var duration = new DurationAccumulator();
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var selector = new ThrowingSelector();
        var aspect = Aspect.MeasureDuration(selector, duration, timeProvider);
        var executor = ResolutionScope.Create(aspect).ResolveSelector(selector);
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() =>
            executor.Select(CreateEvaluatedCandidates([1]), problem.Objective, 1, RandomNumberGenerator.Create(1), problem.SearchSpace, problem));

        duration.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void CountReplacerCalls_IncrementsOncePerReplaceCall()
    {
        var counter = new CountAccumulator();
        var replacer = new FirstReplacementCandidatesReplacer();
        var aspect = Aspect.CountCalls(replacer, counter);
        aspect.Advices.Single().Advice.ShouldBeOfType<CountingReplacerAdvice<int>>().Counter.ShouldBeSameAs(counter);
        aspect.Advices.Single().Advice.ShouldBeOfType<CountingReplacerAdvice<int>>().Metric.ShouldBe(OperatorCountMetric.Calls);
        var executor = ResolutionScope.Create(aspect).ResolveReplacer(replacer);
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
        var aspect = Aspect.CountCandidates(replacer, counter);
        var executor = ResolutionScope.Create(aspect).ResolveReplacer(replacer);
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
        var executor = ResolutionScope.Create(Aspect.CountCalls(source, counter)).ResolveReplacer(source);
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
        var duration = new DurationAccumulator();
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var replacer = new FirstReplacementCandidatesReplacer();
        var aspect = Aspect.MeasureDuration(replacer, duration, timeProvider);
        aspect.Advices.Single().Advice.ShouldBeOfType<DurationMeasuringReplacerAdvice<int>>().Duration.ShouldBeSameAs(duration);
        aspect.Advices.Single().Advice.ShouldBeOfType<DurationMeasuringReplacerAdvice<int>>().TimeProvider.ShouldBeSameAs(timeProvider);
        var executor = ResolutionScope.Create(aspect).ResolveReplacer(replacer);
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

        duration.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(6));
    }

    [Fact]
    public void MeasureReplacerDuration_RecordsFailedCall()
    {
        var duration = new DurationAccumulator();
        var source = new ThrowingReplacer();
        var executor = ResolutionScope.Create(Aspect.MeasureDuration(source, duration, new AdvancingTimeProvider(TimeSpan.FromSeconds(3)))).ResolveReplacer(source);
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() => executor.Replace(
            CreateEvaluatedCandidates([1]),
            CreateEvaluatedCandidates([2]),
            problem.Objective,
            1,
            RandomNumberGenerator.Create(1),
            problem.SearchSpace,
            problem));

        duration.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void CountInterceptorCalls_IncrementsOncePerTransformCall()
    {
        var counter = new CountAccumulator();
        var interceptor = new AddOneInterceptor();
        var aspect = Aspect.CountCalls(interceptor, counter);
        aspect.Advices.Single().Advice.ShouldBeOfType<CountingInterceptorAdvice<int>>().Counter.ShouldBeSameAs(counter);
        var execution = ResolutionScope.Create(aspect).Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, CounterState>(interceptor);
        var problem = CreateProblem();

        execution.Transform(new CounterState { Value = 1 }, previousState: null, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        execution.Transform(new CounterState { Value = 2 }, new CounterState { Value = 1 }, RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        counter.CurrentCount.ShouldBe(2);
    }

    [Fact]
    public void MeasureInterceptorDuration_AddsElapsedInterceptorExecutionDuration()
    {
        var duration = new DurationAccumulator();
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var interceptor = new AddOneInterceptor();
        var aspect = Aspect.MeasureDuration(interceptor, duration, timeProvider);
        aspect.Advices.Single().Advice.ShouldBeOfType<DurationMeasuringInterceptorAdvice<int>>().Duration.ShouldBeSameAs(duration);
        aspect.Advices.Single().Advice.ShouldBeOfType<DurationMeasuringInterceptorAdvice<int>>().TimeProvider.ShouldBeSameAs(timeProvider);
        var execution = ResolutionScope.Create(aspect).Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, CounterState>(interceptor);
        var problem = CreateProblem();

        execution.Transform(new CounterState { Value = 1 }, previousState: null, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        execution.Transform(new CounterState { Value = 2 }, new CounterState { Value = 1 }, RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        duration.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(6));
    }

    [Fact]
    public void CountInterceptorCalls_DoesNotCountFailedCall()
    {
        var counter = new CountAccumulator();
        var source = new ThrowingInterceptor();
        var execution = ResolutionScope.Create(Aspect.CountCalls(source, counter)).Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, CounterState>(source);
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() => execution.Transform(new CounterState { Value = 1 }, null, RandomNumberGenerator.Create(1), problem.SearchSpace, problem));

        counter.CurrentCount.ShouldBe(0);
    }

    [Fact]
    public void MeasureInterceptorDuration_RecordsFailedCall()
    {
        var duration = new DurationAccumulator();
        var source = new ThrowingInterceptor();
        var execution = ResolutionScope.Create(Aspect.MeasureDuration(source, duration, new AdvancingTimeProvider(TimeSpan.FromSeconds(3)))).Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, CounterState>(source);
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() => execution.Transform(new CounterState { Value = 1 }, null, RandomNumberGenerator.Create(1), problem.SearchSpace, problem));

        duration.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void CountTerminatorCalls_IncrementsOncePerTerminalStateCheck()
    {
        var counter = new CountAccumulator();
        var terminator = new NeverTerminalStateTerminator();
        var aspect = Aspect.CountCalls(terminator, counter);
        aspect.Advices.Single().Advice.ShouldBeOfType<CountingTerminatorAdvice<int>>().Counter.ShouldBeSameAs(counter);
        var execution = ResolutionScope.Create(aspect).Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, CounterState>(terminator);
        var problem = CreateProblem();

        execution.IsTerminalState(new CounterState { Value = 1 }, problem.SearchSpace, problem);
        execution.IsTerminalState(new CounterState { Value = 2 }, problem.SearchSpace, problem);

        counter.CurrentCount.ShouldBe(2);
    }

    [Fact]
    public void MeasureTerminatorDuration_AddsElapsedTerminatorExecutionDuration()
    {
        var duration = new DurationAccumulator();
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var terminator = new NeverTerminalStateTerminator();
        var aspect = Aspect.MeasureDuration(terminator, duration, timeProvider);
        aspect.Advices.Single().Advice.ShouldBeOfType<DurationMeasuringTerminatorAdvice<int>>().Duration.ShouldBeSameAs(duration);
        aspect.Advices.Single().Advice.ShouldBeOfType<DurationMeasuringTerminatorAdvice<int>>().TimeProvider.ShouldBeSameAs(timeProvider);
        var execution = ResolutionScope.Create(aspect).Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, CounterState>(terminator);
        var problem = CreateProblem();

        execution.IsTerminalState(new CounterState { Value = 1 }, problem.SearchSpace, problem);
        execution.IsTerminalState(new CounterState { Value = 2 }, problem.SearchSpace, problem);

        duration.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(6));
    }

    [Fact]
    public void CountTerminatorCalls_DoesNotCountFailedCall()
    {
        var counter = new CountAccumulator();
        var source = new ThrowingTerminator();
        var execution = ResolutionScope.Create(Aspect.CountCalls(source, counter)).Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, CounterState>(source);
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() => execution.IsTerminalState(new CounterState { Value = 1 }, problem.SearchSpace, problem));

        counter.CurrentCount.ShouldBe(0);
    }

    [Fact]
    public void MeasureTerminatorDuration_RecordsFailedCall()
    {
        var duration = new DurationAccumulator();
        var source = new ThrowingTerminator();
        var execution = ResolutionScope.Create(Aspect.MeasureDuration(source, duration, new AdvancingTimeProvider(TimeSpan.FromSeconds(3)))).Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, CounterState>(source);
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() => execution.IsTerminalState(new CounterState { Value = 1 }, problem.SearchSpace, problem));

        duration.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(3));
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
        public OperatorExecutorFactory CreateExecutorFactory()
        {
            var executor = CreateBoundExecution();
            return OperatorExecutorFactory.FromFactory(_ => executor);
        }

        private IMutationExecutor<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> CreateBoundExecution() =>
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
            DummySearchSpace<int> searchSpace,
            FuncProblem<int, DummySearchSpace<int>> problem)
        {
            return false;
        }
    }

    private sealed record ThrowingTerminator
        : StatelessTerminator<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, CounterState>
    {
        public override bool IsTerminalState(CounterState state, DummySearchSpace<int> searchSpace, FuncProblem<int, DummySearchSpace<int>> problem) =>
            throw new InvalidOperationException();
    }

    private sealed class MutationState
    {
        public int Calls { get; set; }
    }

    private sealed record AdvancingMutator(Action Prepare) : StatefulMutator<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, MutationState>
    {
        protected override MutationState CreateInitialState()
        {
            Prepare();
            return new MutationState();
        }

        protected override IReadOnlyList<int> Mutate(IReadOnlyList<int> parents, MutationState state, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace, FuncProblem<int, DummySearchSpace<int>> problem)
        {
            var call = ++state.Calls;
            return parents.Select(parent => parent + call).ToArray();
        }
    }

    private sealed record CounterState : SearchState
    {
        public required int Value { get; init; }
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
        public ICreationExecutor<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> ResolveCreator(ICreator<int> creator) =>
            scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(creator);

        public ICrossoverExecutor<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> ResolveCrossover(ICrossover<int> crossover) =>
            scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(crossover);

        public IMutationExecutor<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> ResolveMutator(IOperator<Mutation, int> mutator) =>
            scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(mutator);

        public ISelectionExecutor<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> ResolveSelector(ISelector<int> selector) =>
            scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(selector);

        public IReplacementExecutor<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> ResolveReplacer(IReplacer<int> replacer) =>
            scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(replacer);
    }

    extension(ICreator<int> creator)
    {
        public ICreationExecutor<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> CreateCreatorExecution() =>
            ResolutionScope.Create().ResolveCreator(creator);
    }

    extension(ICrossover<int> crossover)
    {
        public ICrossoverExecutor<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> CreateCrossoverExecution() =>
            ResolutionScope.Create().ResolveCrossover(crossover);
    }

    extension(IOperator<Mutation, int> mutator)
    {
        public IMutationExecutor<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> CreateMutatorExecution() =>
            ResolutionScope.Create().ResolveMutator(mutator);
    }

    extension(ISelector<int> selector)
    {
        public ISelectionExecutor<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> CreateSelectorExecution() =>
            ResolutionScope.Create().ResolveSelector(selector);
    }

    extension(IReplacer<int> replacer)
    {
        public IReplacementExecutor<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> CreateReplacerExecution() =>
            ResolutionScope.Create().ResolveReplacer(replacer);
    }
}
