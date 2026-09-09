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

public class ObservableOperatorCounterTests
{
    [Fact]
    public void ObservableCreator_SnapshotsObservers()
    {
        var observer = new ActionCreatorObserver<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>((_, _, _, _) => { });
        var observers = new List<ICreatorObserver<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>> { observer };
        var observable = new ObservableCreator<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(new SequenceCreator(), observers);

        observers.Clear();

        observable.Observers.ShouldBe([observer]);
    }

    [Fact]
    public void CountCreatorCalls_IncrementsOncePerCreateCall()
    {
        var counter = new ObservationCounter();
        var creator = new SequenceCreator().CountCalls(counter);
        creator.Counter.ShouldBeSameAs(counter);
        creator.Metric.ShouldBe(OperatorCountMetric.Calls);
        var instance = creator.CreateCreatorInstance();
        var problem = CreateProblem();

        instance.Create(3, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        instance.Create(1, RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        counter.CurrentCount.ShouldBe(2);
    }

    [Fact]
    public void CountCreatedCandidates_IncrementsByReturnedCandidateCount()
    {
        var counter = new ObservationCounter();
        var creator = new SequenceCreator().CountCandidates(counter);
        var instance = creator.CreateCreatorInstance();
        var problem = CreateProblem();

        instance.Create(3, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        instance.Create(1, RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        counter.CurrentCount.ShouldBe(4);
    }

    [Fact]
    public void MeasureCreatorDuration_AddsElapsedCreatorExecutionDuration()
    {
        var duration = new ObservationDuration();
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var creator = new SequenceCreator().MeasureDuration(duration, timeProvider);
        creator.Duration.ShouldBeSameAs(duration);
        creator.TimeProvider.ShouldBeSameAs(timeProvider);
        var instance = creator.CreateCreatorInstance();
        var problem = CreateProblem();

        instance.Create(3, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        instance.Create(1, RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        duration.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(6));
    }

    [Fact]
    public void ObservableCreator_DoesNotInvokeObserversWhenCreationThrows()
    {
        var observed = 0;
        var instance = new ThrowingCreator().ObserveWith(_ => observed++).CreateCreatorInstance();
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() =>
            instance.Create(1, RandomNumberGenerator.Create(1), problem.SearchSpace, problem));

        observed.ShouldBe(0);
    }

    [Fact]
    public void CountCreatorCalls_DoesNotIncrementWhenCreationThrows()
    {
        var counter = new ObservationCounter();
        var instance = new ThrowingCreator().CountCalls(counter).CreateCreatorInstance();
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() =>
            instance.Create(1, RandomNumberGenerator.Create(1), problem.SearchSpace, problem));

        counter.CurrentCount.ShouldBe(0);
    }

    [Fact]
    public void MeasureCreatorDuration_RecordsElapsedDurationWhenCreationThrows()
    {
        var duration = new ObservationDuration();
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var instance = new ThrowingCreator().MeasureDuration(duration, timeProvider).CreateCreatorInstance();
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() =>
            instance.Create(1, RandomNumberGenerator.Create(1), problem.SearchSpace, problem));

        duration.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void ObservableMutator_SnapshotsObservers()
    {
        var observer = new ActionMutatorObserver<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>((_, _, _, _) => { });
        var observers = new List<IMutatorObserver<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>> { observer };
        var observable = new ObservableMutator<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(new AddOneMutator(), observers);

        observers.Clear();

        observable.Observers.ShouldBe([observer]);
    }

    [Fact]
    public void ObservableMutator_InvokesObserversInOrderWithOffspringAndParents()
    {
        var calls = new List<string>();
        IReadOnlyList<int> observedOffspring = [];
        IReadOnlyList<int> observedParents = [];
        var problem = CreateProblem();

        var mutator = new AddOneMutator().ObserveWith(
            new ActionMutatorObserver<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>((offspring, parents, searchSpace, observedProblem) =>
            {
                calls.Add("first");
                observedOffspring = offspring;
                observedParents = parents;
                searchSpace.ShouldBeSameAs(problem.SearchSpace);
                observedProblem.ShouldBeSameAs(problem);
            }),
            new ActionMutatorObserver<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>((_, _, _, _) => calls.Add("second")));

        var result = mutator.CreateMutatorInstance().Mutate([1, 2, 3], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);

        calls.ShouldBe(["first", "second"]);
        observedOffspring.ShouldBeSameAs(result);
        observedOffspring.ShouldBe([2, 3, 4]);
        observedParents.ShouldBe([1, 2, 3]);
    }

    [Fact]
    public void ObservableMutator_DoesNotInvokeObserversWhenMutationThrows()
    {
        var observed = 0;
        var instance = new ThrowingMutator().ObserveWith(_ => observed++).CreateMutatorInstance();
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() =>
            instance.Mutate([1], RandomNumberGenerator.Create(1), problem.SearchSpace, problem));

        observed.ShouldBe(0);
    }

    [Fact]
    public void CountMutatorCalls_IncrementsOncePerMutateCall()
    {
        var counter = new ObservationCounter();
        var mutator = new AddOneMutator().CountCalls(counter);
        mutator.Counter.ShouldBeSameAs(counter);
        mutator.Metric.ShouldBe(OperatorCountMetric.Calls);
        var instance = mutator.CreateMutatorInstance();
        var problem = CreateProblem();

        instance.Mutate([1, 2, 3], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        instance.Mutate([4], RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        counter.CurrentCount.ShouldBe(2);
    }

    [Fact]
    public void CountMutatedCandidates_IncrementsByReturnedCandidateCount()
    {
        var counter = new ObservationCounter();
        var mutator = new AddOneMutator().CountCandidates(counter);
        var instance = mutator.CreateMutatorInstance();
        var problem = CreateProblem();

        instance.Mutate([1, 2, 3], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        instance.Mutate([4], RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        counter.CurrentCount.ShouldBe(4);
    }

    [Fact]
    public void CountMutatorCalls_DoesNotIncrementWhenMutationThrows()
    {
        var counter = new ObservationCounter();
        var instance = new ThrowingMutator().CountCalls(counter).CreateMutatorInstance();
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() =>
            instance.Mutate([1], RandomNumberGenerator.Create(1), problem.SearchSpace, problem));

        counter.CurrentCount.ShouldBe(0);
    }

    [Fact]
    public void MeasureMutatorDuration_AddsElapsedMutatorExecutionDuration()
    {
        var duration = new ObservationDuration();
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var mutator = new AddOneMutator().MeasureDuration(duration, timeProvider);
        mutator.Duration.ShouldBeSameAs(duration);
        mutator.TimeProvider.ShouldBeSameAs(timeProvider);
        var instance = mutator.CreateMutatorInstance();
        var problem = CreateProblem();

        instance.Mutate([1, 2, 3], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        instance.Mutate([4], RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        duration.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(6));
    }

    [Fact]
    public void MeasureMutatorDuration_RecordsElapsedDurationWhenMutationThrows()
    {
        var duration = new ObservationDuration();
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var mutator = new ThrowingMutator().MeasureDuration(duration, timeProvider);
        var instance = mutator.CreateMutatorInstance();
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() =>
            instance.Mutate([1], RandomNumberGenerator.Create(1), problem.SearchSpace, problem));

        duration.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void NestedDurationMeasuringMutators_ForwardTheSameInvocationDataAndInvokeTheChildMutatorOnce()
    {
        var calls = 0;
        IReadOnlyList<int> parents = [1, 2, 3];
        var random = RandomNumberGenerator.Create(1);
        var problem = CreateProblem();
        var innerDuration = new ObservationDuration();
        var outerDuration = new ObservationDuration();
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
        var mutator = childMutator
            .MeasureDuration(innerDuration, timeProvider)
            .MeasureDuration(outerDuration, timeProvider);
        var instance = mutator.CreateMutatorInstance();

        var result = instance.Mutate(parents, random, problem.SearchSpace, problem);

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
        var innerDuration = new ObservationDuration();
        var outerDuration = new ObservationDuration();
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var childMutator = new CallbackMutator((_, _, _, _) =>
        {
            calls++;
            throw new InvalidOperationException();
        });
        var mutator = childMutator
            .MeasureDuration(innerDuration, timeProvider)
            .MeasureDuration(outerDuration, timeProvider);
        var instance = mutator.CreateMutatorInstance();

        Should.Throw<InvalidOperationException>(() =>
            instance.Mutate([1], RandomNumberGenerator.Create(1), problem.SearchSpace, problem));

        calls.ShouldBe(1);
        innerDuration.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(3));
        outerDuration.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(9));
    }

    [Fact]
    public void CountCrossoverCalls_IncrementsOncePerCrossCall()
    {
        var counter = new ObservationCounter();
        var crossover = new SumParentsCrossover().CountCalls(counter);
        crossover.Counter.ShouldBeSameAs(counter);
        crossover.Metric.ShouldBe(OperatorCountMetric.Calls);
        var instance = crossover.CreateCrossoverInstance();
        var problem = CreateProblem();

        instance.Cross(
            [Parents.From(1, 10), Parents.From(2, 20), Parents.From(3, 30)],
            RandomNumberGenerator.Create(1),
            problem.SearchSpace,
            problem);
        instance.Cross(
            [Parents.From(4, 40)],
            RandomNumberGenerator.Create(2),
            problem.SearchSpace,
            problem);

        counter.CurrentCount.ShouldBe(2);
    }

    [Fact]
    public void CountCrossedCandidates_IncrementsByReturnedCandidateCount()
    {
        var counter = new ObservationCounter();
        var crossover = new SumParentsCrossover().CountCandidates(counter);
        var instance = crossover.CreateCrossoverInstance();
        var problem = CreateProblem();

        instance.Cross(
            [Parents.From(1, 10), Parents.From(2, 20), Parents.From(3, 30)],
            RandomNumberGenerator.Create(1),
            problem.SearchSpace,
            problem);
        instance.Cross(
            [Parents.From(4, 40)],
            RandomNumberGenerator.Create(2),
            problem.SearchSpace,
            problem);

        counter.CurrentCount.ShouldBe(4);
    }

    [Fact]
    public void MeasureCrossoverDuration_AddsElapsedCrossoverExecutionDuration()
    {
        var duration = new ObservationDuration();
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var crossover = new SumParentsCrossover().MeasureDuration(duration, timeProvider);
        crossover.Duration.ShouldBeSameAs(duration);
        crossover.TimeProvider.ShouldBeSameAs(timeProvider);
        var instance = crossover.CreateCrossoverInstance();
        var problem = CreateProblem();

        instance.Cross(
            [Parents.From(1, 10), Parents.From(2, 20), Parents.From(3, 30)],
            RandomNumberGenerator.Create(1),
            problem.SearchSpace,
            problem);
        instance.Cross(
            [Parents.From(4, 40)],
            RandomNumberGenerator.Create(2),
            problem.SearchSpace,
            problem);

        duration.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(6));
    }

    [Fact]
    public void ObservableCrossover_DoesNotInvokeObserversWhenCrossoverThrows()
    {
        var observed = 0;
        var instance = new ThrowingCrossover().ObserveWith(_ => observed++).CreateCrossoverInstance();
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() =>
            instance.Cross([Parents.From(1, 10)], RandomNumberGenerator.Create(1), problem.SearchSpace, problem));

        observed.ShouldBe(0);
    }

    [Fact]
    public void CountCrossoverCalls_DoesNotIncrementWhenCrossoverThrows()
    {
        var counter = new ObservationCounter();
        var instance = new ThrowingCrossover().CountCalls(counter).CreateCrossoverInstance();
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() =>
            instance.Cross([Parents.From(1, 10)], RandomNumberGenerator.Create(1), problem.SearchSpace, problem));

        counter.CurrentCount.ShouldBe(0);
    }

    [Fact]
    public void MeasureCrossoverDuration_RecordsElapsedDurationWhenCrossoverThrows()
    {
        var duration = new ObservationDuration();
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var instance = new ThrowingCrossover().MeasureDuration(duration, timeProvider).CreateCrossoverInstance();
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() =>
            instance.Cross([Parents.From(1, 10)], RandomNumberGenerator.Create(1), problem.SearchSpace, problem));

        duration.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void CountSelectorCalls_IncrementsOncePerSelectCall()
    {
        var counter = new ObservationCounter();
        var selector = new FirstCandidatesSelector().CountCalls(counter);
        selector.Counter.ShouldBeSameAs(counter);
        selector.Metric.ShouldBe(OperatorCountMetric.Calls);
        var instance = selector.CreateSelectorInstance();
        var problem = CreateProblem();

        instance.Select(CreateEvaluatedCandidates([1, 2, 3]), problem.Objective, 2, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        instance.Select(CreateEvaluatedCandidates([4]), problem.Objective, 1, RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        counter.CurrentCount.ShouldBe(2);
    }

    [Fact]
    public void ObservableSelector_SnapshotsObservers()
    {
        var observer = new ActionSelectorObserver<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>((_, _, _, _, _, _) => { });
        var observers = new List<ISelectorObserver<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>> { observer };
        var observable = new ObservableSelector<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(new FirstCandidatesSelector(), observers);

        observers.Clear();

        observable.Observers.ShouldBe([observer]);
    }

    [Fact]
    public void ObservableSelector_NormalizesDefaultObserverArrayToEmpty()
    {
        ImmutableArray<ISelectorObserver<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>> observers = default;

        var observable = new ObservableSelector<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(new FirstCandidatesSelector(), observers);

        observable.Observers.IsEmpty.ShouldBeTrue();
        observable.Observers.ShouldBeEmpty();
    }

    [Fact]
    public void ObservableSelector_InvokesObserversInOrderWithSelectionAndPopulation()
    {
        var calls = new List<string>();
        IReadOnlyList<EvaluatedCandidate<int>> observedSelection = [];
        IReadOnlyList<EvaluatedCandidate<int>> observedPopulation = [];
        var population = CreateEvaluatedCandidates([1, 2, 3]);
        var problem = CreateProblem();

        var selector = new FirstCandidatesSelector().ObserveWith(
            new ActionSelectorObserver<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>((selected, candidates, objective, count, searchSpace, observedProblem) =>
            {
                calls.Add("first");
                observedSelection = selected;
                observedPopulation = candidates;
                objective.ShouldBe(problem.Objective);
                count.ShouldBe(2);
                searchSpace.ShouldBeSameAs(problem.SearchSpace);
                observedProblem.ShouldBeSameAs(problem);
            }),
            new ActionSelectorObserver<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>((_, _, _, _, _, _) => calls.Add("second")));

        var result = selector.CreateSelectorInstance().Select(population, problem.Objective, 2, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);

        calls.ShouldBe(["first", "second"]);
        observedSelection.ShouldBeSameAs(result);
        observedSelection.ShouldBe([population[0], population[1]]);
        observedPopulation.ShouldBeSameAs(population);
    }

    [Fact]
    public void ObservableSelector_DoesNotInvokeObserversWhenSelectionThrows()
    {
        var observed = 0;
        var instance = new ThrowingSelector().ObserveWith(_ => observed++).CreateSelectorInstance();
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() =>
            instance.Select(CreateEvaluatedCandidates([1]), problem.Objective, 1, RandomNumberGenerator.Create(1), problem.SearchSpace, problem));

        observed.ShouldBe(0);
    }

    [Fact]
    public void CountSelectedCandidates_IncrementsByReturnedCandidateCount()
    {
        var counter = new ObservationCounter();
        var selector = new FirstCandidatesSelector().CountCandidates(counter);
        var instance = selector.CreateSelectorInstance();
        var problem = CreateProblem();

        instance.Select(CreateEvaluatedCandidates([1, 2, 3]), problem.Objective, 2, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        instance.Select(CreateEvaluatedCandidates([4]), problem.Objective, 1, RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        counter.CurrentCount.ShouldBe(3);
    }

    [Fact]
    public void CountSelectorCalls_DoesNotIncrementWhenSelectionThrows()
    {
        var counter = new ObservationCounter();
        var instance = new ThrowingSelector().CountCalls(counter).CreateSelectorInstance();
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() =>
            instance.Select(CreateEvaluatedCandidates([1]), problem.Objective, 1, RandomNumberGenerator.Create(1), problem.SearchSpace, problem));

        counter.CurrentCount.ShouldBe(0);
    }

    [Fact]
    public void MeasureSelectorDuration_AddsElapsedSelectorExecutionDuration()
    {
        var duration = new ObservationDuration();
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var selector = new FirstCandidatesSelector().MeasureDuration(duration, timeProvider);
        selector.Duration.ShouldBeSameAs(duration);
        selector.TimeProvider.ShouldBeSameAs(timeProvider);
        var instance = selector.CreateSelectorInstance();
        var problem = CreateProblem();

        instance.Select(CreateEvaluatedCandidates([1, 2, 3]), problem.Objective, 2, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        instance.Select(CreateEvaluatedCandidates([4]), problem.Objective, 1, RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        duration.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(6));
    }

    [Fact]
    public void MeasureSelectorDuration_RecordsElapsedDurationWhenSelectionThrows()
    {
        var duration = new ObservationDuration();
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var selector = new ThrowingSelector().MeasureDuration(duration, timeProvider);
        var instance = selector.CreateSelectorInstance();
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() =>
            instance.Select(CreateEvaluatedCandidates([1]), problem.Objective, 1, RandomNumberGenerator.Create(1), problem.SearchSpace, problem));

        duration.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void CountReplacerCalls_IncrementsOncePerReplaceCall()
    {
        var counter = new ObservationCounter();
        var replacer = new FirstReplacementCandidatesReplacer().CountCalls(counter);
        replacer.Counter.ShouldBeSameAs(counter);
        replacer.Metric.ShouldBe(OperatorCountMetric.Calls);
        var instance = replacer.CreateReplacerInstance();
        var problem = CreateProblem();

        instance.Replace(
            CreateEvaluatedCandidates([1, 2, 3]),
            CreateEvaluatedCandidates([10, 20]),
            problem.Objective,
            2,
            RandomNumberGenerator.Create(1),
            problem.SearchSpace,
            problem);
        instance.Replace(
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
        var counter = new ObservationCounter();
        var replacer = new FirstReplacementCandidatesReplacer().CountCandidates(counter);
        var instance = replacer.CreateReplacerInstance();
        var problem = CreateProblem();

        instance.Replace(
            CreateEvaluatedCandidates([1, 2, 3]),
            CreateEvaluatedCandidates([10, 20]),
            problem.Objective,
            2,
            RandomNumberGenerator.Create(1),
            problem.SearchSpace,
            problem);
        instance.Replace(
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
    public void ObservableReplacer_DoesNotInvokeObserversWhenReplacementThrows()
    {
        var observed = 0;
        var instance = new ThrowingReplacer().ObserveWith(_ => observed++).CreateReplacerInstance();
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() => instance.Replace(
            CreateEvaluatedCandidates([1]),
            CreateEvaluatedCandidates([2]),
            problem.Objective,
            1,
            RandomNumberGenerator.Create(1),
            problem.SearchSpace,
            problem));

        observed.ShouldBe(0);
    }

    [Fact]
    public void CountReplacerCalls_DoesNotCountFailedCall()
    {
        var counter = new ObservationCounter();
        var instance = new ThrowingReplacer().CountCalls(counter).CreateReplacerInstance();
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() => instance.Replace(
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
        var duration = new ObservationDuration();
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var replacer = new FirstReplacementCandidatesReplacer().MeasureDuration(duration, timeProvider);
        replacer.Duration.ShouldBeSameAs(duration);
        replacer.TimeProvider.ShouldBeSameAs(timeProvider);
        var instance = replacer.CreateReplacerInstance();
        var problem = CreateProblem();

        instance.Replace(
            CreateEvaluatedCandidates([1, 2, 3]),
            CreateEvaluatedCandidates([10, 20]),
            problem.Objective,
            2,
            RandomNumberGenerator.Create(1),
            problem.SearchSpace,
            problem);
        instance.Replace(
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
        var duration = new ObservationDuration();
        var instance = new ThrowingReplacer().MeasureDuration(duration, new AdvancingTimeProvider(TimeSpan.FromSeconds(3))).CreateReplacerInstance();
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() => instance.Replace(
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
        var counter = new ObservationCounter();
        var interceptor = new AddOneInterceptor().CountCalls(counter);
        interceptor.Counter.ShouldBeSameAs(counter);
        var instance = interceptor.CreateExecutionInstance<DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, CounterState>(new ExecutionInstanceRegistry());
        var problem = CreateProblem();

        instance.Transform(new CounterState { Value = 1 }, previousState: null, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        instance.Transform(new CounterState { Value = 2 }, new CounterState { Value = 1 }, RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        counter.CurrentCount.ShouldBe(2);
    }

    [Fact]
    public void MeasureInterceptorDuration_AddsElapsedInterceptorExecutionDuration()
    {
        var duration = new ObservationDuration();
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var interceptor = new AddOneInterceptor().MeasureDuration(duration, timeProvider);
        interceptor.Duration.ShouldBeSameAs(duration);
        interceptor.TimeProvider.ShouldBeSameAs(timeProvider);
        var instance = interceptor.CreateExecutionInstance<DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, CounterState>(new ExecutionInstanceRegistry());
        var problem = CreateProblem();

        instance.Transform(new CounterState { Value = 1 }, previousState: null, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        instance.Transform(new CounterState { Value = 2 }, new CounterState { Value = 1 }, RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        duration.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(6));
    }

    [Fact]
    public void ObservableInterceptor_DoesNotInvokeObserversWhenTransformThrows()
    {
        var observed = 0;
        var instance = new ThrowingInterceptor().ObserveWith((CounterState _) => observed++).CreateExecutionInstance<DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, CounterState>(new ExecutionInstanceRegistry());
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() => instance.Transform(new CounterState { Value = 1 }, null, RandomNumberGenerator.Create(1), problem.SearchSpace, problem));

        observed.ShouldBe(0);
    }

    [Fact]
    public void CountInterceptorCalls_DoesNotCountFailedCall()
    {
        var counter = new ObservationCounter();
        var instance = new ThrowingInterceptor().CountCalls(counter).CreateExecutionInstance<DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, CounterState>(new ExecutionInstanceRegistry());
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() => instance.Transform(new CounterState { Value = 1 }, null, RandomNumberGenerator.Create(1), problem.SearchSpace, problem));

        counter.CurrentCount.ShouldBe(0);
    }

    [Fact]
    public void MeasureInterceptorDuration_RecordsFailedCall()
    {
        var duration = new ObservationDuration();
        var instance = new ThrowingInterceptor().MeasureDuration(duration, new AdvancingTimeProvider(TimeSpan.FromSeconds(3))).CreateExecutionInstance<DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, CounterState>(new ExecutionInstanceRegistry());
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() => instance.Transform(new CounterState { Value = 1 }, null, RandomNumberGenerator.Create(1), problem.SearchSpace, problem));

        duration.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void CountTerminatorCalls_IncrementsOncePerTerminalStateCheck()
    {
        var counter = new ObservationCounter();
        var terminator = new NeverTerminalStateTerminator().CountCalls(counter);
        terminator.Counter.ShouldBeSameAs(counter);
        var instance = terminator.CreateExecutionInstance<DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, CounterState>(new ExecutionInstanceRegistry());
        var problem = CreateProblem();

        instance.IsTerminalState(new CounterState { Value = 1 }, problem.SearchSpace, problem);
        instance.IsTerminalState(new CounterState { Value = 2 }, problem.SearchSpace, problem);

        counter.CurrentCount.ShouldBe(2);
    }

    [Fact]
    public void MeasureTerminatorDuration_AddsElapsedTerminatorExecutionDuration()
    {
        var duration = new ObservationDuration();
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var terminator = new NeverTerminalStateTerminator().MeasureDuration(duration, timeProvider);
        terminator.Duration.ShouldBeSameAs(duration);
        terminator.TimeProvider.ShouldBeSameAs(timeProvider);
        var instance = terminator.CreateExecutionInstance<DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, CounterState>(new ExecutionInstanceRegistry());
        var problem = CreateProblem();

        instance.IsTerminalState(new CounterState { Value = 1 }, problem.SearchSpace, problem);
        instance.IsTerminalState(new CounterState { Value = 2 }, problem.SearchSpace, problem);

        duration.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(6));
    }

    [Fact]
    public void ObservableTerminator_DoesNotInvokeObserversWhenTerminalStateCheckThrows()
    {
        var observed = 0;
        var instance = new ThrowingTerminator().ObserveWith(_ => observed++).CreateExecutionInstance<DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, CounterState>(new ExecutionInstanceRegistry());
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() => instance.IsTerminalState(new CounterState { Value = 1 }, problem.SearchSpace, problem));

        observed.ShouldBe(0);
    }

    [Fact]
    public void CountTerminatorCalls_DoesNotCountFailedCall()
    {
        var counter = new ObservationCounter();
        var instance = new ThrowingTerminator().CountCalls(counter).CreateExecutionInstance<DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, CounterState>(new ExecutionInstanceRegistry());
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() => instance.IsTerminalState(new CounterState { Value = 1 }, problem.SearchSpace, problem));

        counter.CurrentCount.ShouldBe(0);
    }

    [Fact]
    public void MeasureTerminatorDuration_RecordsFailedCall()
    {
        var duration = new ObservationDuration();
        var instance = new ThrowingTerminator().MeasureDuration(duration, new AdvancingTimeProvider(TimeSpan.FromSeconds(3))).CreateExecutionInstance<DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, CounterState>(new ExecutionInstanceRegistry());
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() => instance.IsTerminalState(new CounterState { Value = 1 }, problem.SearchSpace, problem));

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
        : IMutator<int>
    {
        public IMutatorInstance<int, TSearchSpace, TProblem> CreateExecutionInstance<TSearchSpace, TProblem>(ExecutionInstanceRegistry instanceRegistry)
            where TSearchSpace : class, ISearchSpace<int>
            where TProblem : class, IProblem<int, TSearchSpace> =>
            (IMutatorInstance<int, TSearchSpace, TProblem>)CreateBoundInstance();

        private IMutatorInstance<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> CreateBoundInstance() =>
            new Instance(callback);

        private sealed class Instance(MutateCallback callback)
            : IMutatorInstance<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>
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
    extension(ExecutionInstanceRegistry registry)
    {
        public ICreatorInstance<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> ResolveCreator(ICreator<int> creator) =>
            registry.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(creator);

        public ICrossoverInstance<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> ResolveCrossover(ICrossover<int> crossover) =>
            registry.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(crossover);

        public IMutatorInstance<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> ResolveMutator(IMutator<int> mutator) =>
            registry.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(mutator);

        public ISelectorInstance<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> ResolveSelector(ISelector<int> selector) =>
            registry.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(selector);

        public IReplacerInstance<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> ResolveReplacer(IReplacer<int> replacer) =>
            registry.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(replacer);
    }

    extension(ICreator<int> creator)
    {
        public ICreatorInstance<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> CreateCreatorInstance() =>
            new ExecutionInstanceRegistry().ResolveCreator(creator);
    }

    extension(ICrossover<int> crossover)
    {
        public ICrossoverInstance<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> CreateCrossoverInstance() =>
            new ExecutionInstanceRegistry().ResolveCrossover(crossover);
    }

    extension(IMutator<int> mutator)
    {
        public IMutatorInstance<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> CreateMutatorInstance() =>
            new ExecutionInstanceRegistry().ResolveMutator(mutator);
    }

    extension(ISelector<int> selector)
    {
        public ISelectorInstance<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> CreateSelectorInstance() =>
            new ExecutionInstanceRegistry().ResolveSelector(selector);
    }

    extension(IReplacer<int> replacer)
    {
        public IReplacerInstance<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> CreateReplacerInstance() =>
            new ExecutionInstanceRegistry().ResolveReplacer(replacer);
    }
}
