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

public class OperatorInstrumentationTests
{

    [Fact]
    public void CountCreatorCalls_IncrementsOncePerCreateCall()
    {
        var counter = new CountAccumulator();
        var creator = new SequenceCreator().CountCalls(counter);
        creator.Counter.ShouldBeSameAs(counter);
        creator.Metric.ShouldBe(OperatorCountMetric.Calls);
        var execution = creator.CreateCreatorExecution();
        var problem = CreateProblem();

        execution.Create(3, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        execution.Create(1, RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        counter.CurrentCount.ShouldBe(2);
    }

    [Fact]
    public void CountCreatedCandidates_IncrementsByReturnedCandidateCount()
    {
        var counter = new CountAccumulator();
        var creator = new SequenceCreator().CountCandidates(counter);
        var execution = creator.CreateCreatorExecution();
        var problem = CreateProblem();

        execution.Create(3, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        execution.Create(1, RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        counter.CurrentCount.ShouldBe(4);
    }

    [Fact]
    public void MeasureCreatorDuration_AddsElapsedCreatorExecutionDuration()
    {
        var duration = new DurationAccumulator();
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var creator = new SequenceCreator().MeasureDuration(duration, timeProvider);
        creator.Duration.ShouldBeSameAs(duration);
        creator.TimeProvider.ShouldBeSameAs(timeProvider);
        var execution = creator.CreateCreatorExecution();
        var problem = CreateProblem();

        execution.Create(3, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        execution.Create(1, RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        duration.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(6));
    }

    [Fact]
    public void CountCreatorCalls_DoesNotIncrementWhenCreationThrows()
    {
        var counter = new CountAccumulator();
        var execution = new ThrowingCreator().CountCalls(counter).CreateCreatorExecution();
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() =>
            execution.Create(1, RandomNumberGenerator.Create(1), problem.SearchSpace, problem));

        counter.CurrentCount.ShouldBe(0);
    }

    [Fact]
    public void MeasureCreatorDuration_RecordsElapsedDurationWhenCreationThrows()
    {
        var duration = new DurationAccumulator();
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var execution = new ThrowingCreator().MeasureDuration(duration, timeProvider).CreateCreatorExecution();
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() =>
            execution.Create(1, RandomNumberGenerator.Create(1), problem.SearchSpace, problem));

        duration.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void CountMutatorCalls_IncrementsOncePerMutateCall()
    {
        var counter = new CountAccumulator();
        var mutator = new AddOneMutator().CountCalls(counter);
        mutator.Counter.ShouldBeSameAs(counter);
        mutator.Metric.ShouldBe(OperatorCountMetric.Calls);
        var execution = mutator.CreateMutatorExecution();
        var problem = CreateProblem();

        execution.Mutate([1, 2, 3], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        execution.Mutate([4], RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        counter.CurrentCount.ShouldBe(2);
    }

    [Fact]
    public void CountMutatedCandidates_IncrementsByReturnedCandidateCount()
    {
        var counter = new CountAccumulator();
        var mutator = new AddOneMutator().CountCandidates(counter);
        var execution = mutator.CreateMutatorExecution();
        var problem = CreateProblem();

        execution.Mutate([1, 2, 3], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        execution.Mutate([4], RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        counter.CurrentCount.ShouldBe(4);
    }

    [Fact]
    public void CountMutatorCalls_DoesNotIncrementWhenMutationThrows()
    {
        var counter = new CountAccumulator();
        var execution = new ThrowingMutator().CountCalls(counter).CreateMutatorExecution();
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() =>
            execution.Mutate([1], RandomNumberGenerator.Create(1), problem.SearchSpace, problem));

        counter.CurrentCount.ShouldBe(0);
    }

    [Fact]
    public void MeasureMutatorDuration_AddsElapsedMutatorExecutionDuration()
    {
        var duration = new DurationAccumulator();
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var mutator = new AddOneMutator().MeasureDuration(duration, timeProvider);
        mutator.Duration.ShouldBeSameAs(duration);
        mutator.TimeProvider.ShouldBeSameAs(timeProvider);
        var execution = mutator.CreateMutatorExecution();
        var problem = CreateProblem();

        execution.Mutate([1, 2, 3], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        execution.Mutate([4], RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        duration.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(6));
    }

    [Fact]
    public void MeasureMutatorDuration_RecordsElapsedDurationWhenMutationThrows()
    {
        var duration = new DurationAccumulator();
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var mutator = new ThrowingMutator().MeasureDuration(duration, timeProvider);
        var execution = mutator.CreateMutatorExecution();
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() =>
            execution.Mutate([1], RandomNumberGenerator.Create(1), problem.SearchSpace, problem));

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
        var mutator = childMutator
            .MeasureDuration(innerDuration, timeProvider)
            .MeasureDuration(outerDuration, timeProvider);
        var execution = mutator.CreateMutatorExecution();

        var result = execution.Mutate(parents, random, problem.SearchSpace, problem);

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
        var mutator = childMutator
            .MeasureDuration(innerDuration, timeProvider)
            .MeasureDuration(outerDuration, timeProvider);
        var execution = mutator.CreateMutatorExecution();

        Should.Throw<InvalidOperationException>(() =>
            execution.Mutate([1], RandomNumberGenerator.Create(1), problem.SearchSpace, problem));

        calls.ShouldBe(1);
        innerDuration.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(3));
        outerDuration.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(9));
    }

    [Fact]
    public void CountCrossoverCalls_IncrementsOncePerCrossCall()
    {
        var counter = new CountAccumulator();
        var crossover = new SumParentsCrossover().CountCalls(counter);
        crossover.Counter.ShouldBeSameAs(counter);
        crossover.Metric.ShouldBe(OperatorCountMetric.Calls);
        var execution = crossover.CreateCrossoverExecution();
        var problem = CreateProblem();

        execution.Cross(
            [Parents.From(1, 10), Parents.From(2, 20), Parents.From(3, 30)],
            RandomNumberGenerator.Create(1),
            problem.SearchSpace,
            problem);
        execution.Cross(
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
        var crossover = new SumParentsCrossover().CountCandidates(counter);
        var execution = crossover.CreateCrossoverExecution();
        var problem = CreateProblem();

        execution.Cross(
            [Parents.From(1, 10), Parents.From(2, 20), Parents.From(3, 30)],
            RandomNumberGenerator.Create(1),
            problem.SearchSpace,
            problem);
        execution.Cross(
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
        var crossover = new SumParentsCrossover().MeasureDuration(duration, timeProvider);
        crossover.Duration.ShouldBeSameAs(duration);
        crossover.TimeProvider.ShouldBeSameAs(timeProvider);
        var execution = crossover.CreateCrossoverExecution();
        var problem = CreateProblem();

        execution.Cross(
            [Parents.From(1, 10), Parents.From(2, 20), Parents.From(3, 30)],
            RandomNumberGenerator.Create(1),
            problem.SearchSpace,
            problem);
        execution.Cross(
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
        var execution = new ThrowingCrossover().CountCalls(counter).CreateCrossoverExecution();
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() =>
            execution.Cross([Parents.From(1, 10)], RandomNumberGenerator.Create(1), problem.SearchSpace, problem));

        counter.CurrentCount.ShouldBe(0);
    }

    [Fact]
    public void MeasureCrossoverDuration_RecordsElapsedDurationWhenCrossoverThrows()
    {
        var duration = new DurationAccumulator();
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var execution = new ThrowingCrossover().MeasureDuration(duration, timeProvider).CreateCrossoverExecution();
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() =>
            execution.Cross([Parents.From(1, 10)], RandomNumberGenerator.Create(1), problem.SearchSpace, problem));

        duration.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void CountSelectorCalls_IncrementsOncePerSelectCall()
    {
        var counter = new CountAccumulator();
        var selector = new FirstCandidatesSelector().CountCalls(counter);
        selector.Counter.ShouldBeSameAs(counter);
        selector.Metric.ShouldBe(OperatorCountMetric.Calls);
        var execution = selector.CreateSelectorExecution();
        var problem = CreateProblem();

        execution.Select(CreateEvaluatedCandidates([1, 2, 3]), problem.Objective, 2, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        execution.Select(CreateEvaluatedCandidates([4]), problem.Objective, 1, RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        counter.CurrentCount.ShouldBe(2);
    }

    [Fact]
    public void CountSelectedCandidates_IncrementsByReturnedCandidateCount()
    {
        var counter = new CountAccumulator();
        var selector = new FirstCandidatesSelector().CountCandidates(counter);
        var execution = selector.CreateSelectorExecution();
        var problem = CreateProblem();

        execution.Select(CreateEvaluatedCandidates([1, 2, 3]), problem.Objective, 2, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        execution.Select(CreateEvaluatedCandidates([4]), problem.Objective, 1, RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        counter.CurrentCount.ShouldBe(3);
    }

    [Fact]
    public void CountSelectorCalls_DoesNotIncrementWhenSelectionThrows()
    {
        var counter = new CountAccumulator();
        var execution = new ThrowingSelector().CountCalls(counter).CreateSelectorExecution();
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() =>
            execution.Select(CreateEvaluatedCandidates([1]), problem.Objective, 1, RandomNumberGenerator.Create(1), problem.SearchSpace, problem));

        counter.CurrentCount.ShouldBe(0);
    }

    [Fact]
    public void MeasureSelectorDuration_AddsElapsedSelectorExecutionDuration()
    {
        var duration = new DurationAccumulator();
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var selector = new FirstCandidatesSelector().MeasureDuration(duration, timeProvider);
        selector.Duration.ShouldBeSameAs(duration);
        selector.TimeProvider.ShouldBeSameAs(timeProvider);
        var execution = selector.CreateSelectorExecution();
        var problem = CreateProblem();

        execution.Select(CreateEvaluatedCandidates([1, 2, 3]), problem.Objective, 2, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        execution.Select(CreateEvaluatedCandidates([4]), problem.Objective, 1, RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        duration.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(6));
    }

    [Fact]
    public void MeasureSelectorDuration_RecordsElapsedDurationWhenSelectionThrows()
    {
        var duration = new DurationAccumulator();
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var selector = new ThrowingSelector().MeasureDuration(duration, timeProvider);
        var execution = selector.CreateSelectorExecution();
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() =>
            execution.Select(CreateEvaluatedCandidates([1]), problem.Objective, 1, RandomNumberGenerator.Create(1), problem.SearchSpace, problem));

        duration.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void CountReplacerCalls_IncrementsOncePerReplaceCall()
    {
        var counter = new CountAccumulator();
        var replacer = new FirstReplacementCandidatesReplacer().CountCalls(counter);
        replacer.Counter.ShouldBeSameAs(counter);
        replacer.Metric.ShouldBe(OperatorCountMetric.Calls);
        var execution = replacer.CreateReplacerExecution();
        var problem = CreateProblem();

        execution.Replace(
            CreateEvaluatedCandidates([1, 2, 3]),
            CreateEvaluatedCandidates([10, 20]),
            problem.Objective,
            2,
            RandomNumberGenerator.Create(1),
            problem.SearchSpace,
            problem);
        execution.Replace(
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
        var replacer = new FirstReplacementCandidatesReplacer().CountCandidates(counter);
        var execution = replacer.CreateReplacerExecution();
        var problem = CreateProblem();

        execution.Replace(
            CreateEvaluatedCandidates([1, 2, 3]),
            CreateEvaluatedCandidates([10, 20]),
            problem.Objective,
            2,
            RandomNumberGenerator.Create(1),
            problem.SearchSpace,
            problem);
        execution.Replace(
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
        var execution = new ThrowingReplacer().CountCalls(counter).CreateReplacerExecution();
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() => execution.Replace(
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
        var replacer = new FirstReplacementCandidatesReplacer().MeasureDuration(duration, timeProvider);
        replacer.Duration.ShouldBeSameAs(duration);
        replacer.TimeProvider.ShouldBeSameAs(timeProvider);
        var execution = replacer.CreateReplacerExecution();
        var problem = CreateProblem();

        execution.Replace(
            CreateEvaluatedCandidates([1, 2, 3]),
            CreateEvaluatedCandidates([10, 20]),
            problem.Objective,
            2,
            RandomNumberGenerator.Create(1),
            problem.SearchSpace,
            problem);
        execution.Replace(
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
        var execution = new ThrowingReplacer().MeasureDuration(duration, new AdvancingTimeProvider(TimeSpan.FromSeconds(3))).CreateReplacerExecution();
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() => execution.Replace(
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
        var interceptor = new AddOneInterceptor().CountCalls(counter);
        interceptor.Counter.ShouldBeSameAs(counter);
        var execution = interceptor.CreateExecutionInstance<DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, CounterState>(ResolutionScope.Create());
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
        var interceptor = new AddOneInterceptor().MeasureDuration(duration, timeProvider);
        interceptor.Duration.ShouldBeSameAs(duration);
        interceptor.TimeProvider.ShouldBeSameAs(timeProvider);
        var execution = interceptor.CreateExecutionInstance<DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, CounterState>(ResolutionScope.Create());
        var problem = CreateProblem();

        execution.Transform(new CounterState { Value = 1 }, previousState: null, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        execution.Transform(new CounterState { Value = 2 }, new CounterState { Value = 1 }, RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        duration.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(6));
    }

    [Fact]
    public void CountInterceptorCalls_DoesNotCountFailedCall()
    {
        var counter = new CountAccumulator();
        var execution = new ThrowingInterceptor().CountCalls(counter).CreateExecutionInstance<DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, CounterState>(ResolutionScope.Create());
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() => execution.Transform(new CounterState { Value = 1 }, null, RandomNumberGenerator.Create(1), problem.SearchSpace, problem));

        counter.CurrentCount.ShouldBe(0);
    }

    [Fact]
    public void MeasureInterceptorDuration_RecordsFailedCall()
    {
        var duration = new DurationAccumulator();
        var execution = new ThrowingInterceptor().MeasureDuration(duration, new AdvancingTimeProvider(TimeSpan.FromSeconds(3))).CreateExecutionInstance<DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, CounterState>(ResolutionScope.Create());
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() => execution.Transform(new CounterState { Value = 1 }, null, RandomNumberGenerator.Create(1), problem.SearchSpace, problem));

        duration.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void CountTerminatorCalls_IncrementsOncePerTerminalStateCheck()
    {
        var counter = new CountAccumulator();
        var terminator = new NeverTerminalStateTerminator().CountCalls(counter);
        terminator.Counter.ShouldBeSameAs(counter);
        var execution = terminator.CreateExecutionInstance<DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, CounterState>(ResolutionScope.Create());
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
        var terminator = new NeverTerminalStateTerminator().MeasureDuration(duration, timeProvider);
        terminator.Duration.ShouldBeSameAs(duration);
        terminator.TimeProvider.ShouldBeSameAs(timeProvider);
        var execution = terminator.CreateExecutionInstance<DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, CounterState>(ResolutionScope.Create());
        var problem = CreateProblem();

        execution.IsTerminalState(new CounterState { Value = 1 }, problem.SearchSpace, problem);
        execution.IsTerminalState(new CounterState { Value = 2 }, problem.SearchSpace, problem);

        duration.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(6));
    }

    [Fact]
    public void CountTerminatorCalls_DoesNotCountFailedCall()
    {
        var counter = new CountAccumulator();
        var execution = new ThrowingTerminator().CountCalls(counter).CreateExecutionInstance<DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, CounterState>(ResolutionScope.Create());
        var problem = CreateProblem();

        Should.Throw<InvalidOperationException>(() => execution.IsTerminalState(new CounterState { Value = 1 }, problem.SearchSpace, problem));

        counter.CurrentCount.ShouldBe(0);
    }

    [Fact]
    public void MeasureTerminatorDuration_RecordsFailedCall()
    {
        var duration = new DurationAccumulator();
        var execution = new ThrowingTerminator().MeasureDuration(duration, new AdvancingTimeProvider(TimeSpan.FromSeconds(3))).CreateExecutionInstance<DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, CounterState>(ResolutionScope.Create());
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
        : IMutator<int>
    {
        public IMutatorExecution<int, TSearchSpace, TProblem> CreateExecutionInstance<TSearchSpace, TProblem>(ResolutionScope scope)
            where TSearchSpace : class, ISearchSpace<int>
            where TProblem : class, IProblem<int, TSearchSpace> =>
            (IMutatorExecution<int, TSearchSpace, TProblem>)CreateBoundExecution();

        private IMutatorExecution<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> CreateBoundExecution() =>
            new Execution(callback);

        private sealed class Execution(MutateCallback callback)
            : IMutatorExecution<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>
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
    extension(ResolutionScope scope)
    {
        public ICreatorExecution<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> ResolveCreator(ICreator<int> creator) =>
            scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(creator);

        public ICrossoverExecution<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> ResolveCrossover(ICrossover<int> crossover) =>
            scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(crossover);

        public IMutatorExecution<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> ResolveMutator(IMutator<int> mutator) =>
            scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(mutator);

        public ISelectorExecution<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> ResolveSelector(ISelector<int> selector) =>
            scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(selector);

        public IReplacerExecution<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> ResolveReplacer(IReplacer<int> replacer) =>
            scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(replacer);
    }

    extension(ICreator<int> creator)
    {
        public ICreatorExecution<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> CreateCreatorExecution() =>
            ResolutionScope.Create().ResolveCreator(creator);
    }

    extension(ICrossover<int> crossover)
    {
        public ICrossoverExecution<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> CreateCrossoverExecution() =>
            ResolutionScope.Create().ResolveCrossover(crossover);
    }

    extension(IMutator<int> mutator)
    {
        public IMutatorExecution<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> CreateMutatorExecution() =>
            ResolutionScope.Create().ResolveMutator(mutator);
    }

    extension(ISelector<int> selector)
    {
        public ISelectorExecution<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> CreateSelectorExecution() =>
            ResolutionScope.Create().ResolveSelector(selector);
    }

    extension(IReplacer<int> replacer)
    {
        public IReplacerExecution<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> CreateReplacerExecution() =>
            ResolutionScope.Create().ResolveReplacer(replacer);
    }
}
