using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Replacers;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.States;
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
        var creator = new SequenceCreator().CountCreatorCalls(counter);
        creator.Counter.ShouldBeSameAs(counter);
        creator.Metric.ShouldBe(OperatorCountMetric.Calls);
        var instance = creator.CreateExecutionInstance();
        var problem = CreateProblem();

        instance.Create(3, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        instance.Create(1, RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        counter.CurrentCount.ShouldBe(2);
    }

    [Fact]
    public void CountCreatedCandidates_IncrementsByReturnedCandidateCount()
    {
        var counter = new ObservationCounter();
        var creator = new SequenceCreator().CountCreatedCandidates(counter);
        var instance = creator.CreateExecutionInstance();
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
        var creator = new SequenceCreator().MeasureCreatorDuration(duration, timeProvider);
        creator.Duration.ShouldBeSameAs(duration);
        creator.TimeProvider.ShouldBeSameAs(timeProvider);
        var instance = creator.CreateExecutionInstance();
        var problem = CreateProblem();

        instance.Create(3, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        instance.Create(1, RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        duration.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(6));
    }

    [Fact]
    public void CountMutatorCalls_IncrementsOncePerMutateCall()
    {
        var counter = new ObservationCounter();
        var mutator = new AddOneMutator().CountMutatorCalls(counter);
        mutator.Counter.ShouldBeSameAs(counter);
        mutator.Metric.ShouldBe(OperatorCountMetric.Calls);
        var instance = mutator.CreateExecutionInstance();
        var problem = CreateProblem();

        instance.Mutate([1, 2, 3], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        instance.Mutate([4], RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        counter.CurrentCount.ShouldBe(2);
    }

    [Fact]
    public void CountMutatedCandidates_IncrementsByReturnedCandidateCount()
    {
        var counter = new ObservationCounter();
        var mutator = new AddOneMutator().CountMutatedCandidates(counter);
        var instance = mutator.CreateExecutionInstance();
        var problem = CreateProblem();

        instance.Mutate([1, 2, 3], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        instance.Mutate([4], RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        counter.CurrentCount.ShouldBe(4);
    }

    [Fact]
    public void MeasureMutatorDuration_AddsElapsedMutatorExecutionDuration()
    {
        var duration = new ObservationDuration();
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var mutator = new AddOneMutator().MeasureMutatorDuration(duration, timeProvider);
        mutator.Duration.ShouldBeSameAs(duration);
        mutator.TimeProvider.ShouldBeSameAs(timeProvider);
        var instance = mutator.CreateExecutionInstance();
        var problem = CreateProblem();

        instance.Mutate([1, 2, 3], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        instance.Mutate([4], RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        duration.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(6));
    }

    [Fact]
    public void CountCrossoverCalls_IncrementsOncePerCrossCall()
    {
        var counter = new ObservationCounter();
        var crossover = new SumParentsCrossover().CountCrossoverCalls(counter);
        crossover.Counter.ShouldBeSameAs(counter);
        crossover.Metric.ShouldBe(OperatorCountMetric.Calls);
        var instance = crossover.CreateExecutionInstance();
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
        var crossover = new SumParentsCrossover().CountCrossedCandidates(counter);
        var instance = crossover.CreateExecutionInstance();
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
        var crossover = new SumParentsCrossover().MeasureCrossoverDuration(duration, timeProvider);
        crossover.Duration.ShouldBeSameAs(duration);
        crossover.TimeProvider.ShouldBeSameAs(timeProvider);
        var instance = crossover.CreateExecutionInstance();
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
    public void CountSelectorCalls_IncrementsOncePerSelectCall()
    {
        var counter = new ObservationCounter();
        var selector = new FirstCandidatesSelector().CountSelectorCalls(counter);
        selector.Counter.ShouldBeSameAs(counter);
        selector.Metric.ShouldBe(OperatorCountMetric.Calls);
        var instance = selector.CreateExecutionInstance();
        var problem = CreateProblem();

        instance.Select(CreateEvaluatedCandidates([1, 2, 3]), problem.Objective, 2, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        instance.Select(CreateEvaluatedCandidates([4]), problem.Objective, 1, RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        counter.CurrentCount.ShouldBe(2);
    }

    [Fact]
    public void CountSelectedCandidates_IncrementsByReturnedCandidateCount()
    {
        var counter = new ObservationCounter();
        var selector = new FirstCandidatesSelector().CountSelectedCandidates(counter);
        var instance = selector.CreateExecutionInstance();
        var problem = CreateProblem();

        instance.Select(CreateEvaluatedCandidates([1, 2, 3]), problem.Objective, 2, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        instance.Select(CreateEvaluatedCandidates([4]), problem.Objective, 1, RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        counter.CurrentCount.ShouldBe(3);
    }

    [Fact]
    public void MeasureSelectorDuration_AddsElapsedSelectorExecutionDuration()
    {
        var duration = new ObservationDuration();
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var selector = new FirstCandidatesSelector().MeasureSelectorDuration(duration, timeProvider);
        selector.Duration.ShouldBeSameAs(duration);
        selector.TimeProvider.ShouldBeSameAs(timeProvider);
        var instance = selector.CreateExecutionInstance();
        var problem = CreateProblem();

        instance.Select(CreateEvaluatedCandidates([1, 2, 3]), problem.Objective, 2, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        instance.Select(CreateEvaluatedCandidates([4]), problem.Objective, 1, RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        duration.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(6));
    }

    [Fact]
    public void CountReplacerCalls_IncrementsOncePerReplaceCall()
    {
        var counter = new ObservationCounter();
        var replacer = new FirstReplacementCandidatesReplacer().CountReplacerCalls(counter);
        replacer.Counter.ShouldBeSameAs(counter);
        replacer.Metric.ShouldBe(OperatorCountMetric.Calls);
        var instance = replacer.CreateExecutionInstance();
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
        var replacer = new FirstReplacementCandidatesReplacer().CountReplacementCandidates(counter);
        var instance = replacer.CreateExecutionInstance();
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
    public void MeasureReplacerDuration_AddsElapsedReplacerExecutionDuration()
    {
        var duration = new ObservationDuration();
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var replacer = new FirstReplacementCandidatesReplacer().MeasureReplacerDuration(duration, timeProvider);
        replacer.Duration.ShouldBeSameAs(duration);
        replacer.TimeProvider.ShouldBeSameAs(timeProvider);
        var instance = replacer.CreateExecutionInstance();
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
    public void CountInterceptorCalls_IncrementsOncePerTransformCall()
    {
        var counter = new ObservationCounter();
        var interceptor = new AddOneInterceptor().CountInterceptorCalls(counter);
        interceptor.Counter.ShouldBeSameAs(counter);
        var instance = interceptor.CreateExecutionInstance();
        var problem = CreateProblem();

        instance.Transform(new CounterState { Value = 1 }, previousState: null, problem.SearchSpace, problem);
        instance.Transform(new CounterState { Value = 2 }, new CounterState { Value = 1 }, problem.SearchSpace, problem);

        counter.CurrentCount.ShouldBe(2);
    }

    [Fact]
    public void MeasureInterceptorDuration_AddsElapsedInterceptorExecutionDuration()
    {
        var duration = new ObservationDuration();
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(3));
        var interceptor = new AddOneInterceptor().MeasureInterceptorDuration(duration, timeProvider);
        interceptor.Duration.ShouldBeSameAs(duration);
        interceptor.TimeProvider.ShouldBeSameAs(timeProvider);
        var instance = interceptor.CreateExecutionInstance();
        var problem = CreateProblem();

        instance.Transform(new CounterState { Value = 1 }, previousState: null, problem.SearchSpace, problem);
        instance.Transform(new CounterState { Value = 2 }, new CounterState { Value = 1 }, problem.SearchSpace, problem);

        duration.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(6));
    }

    [Fact]
    public void CountTerminatorCalls_IncrementsOncePerTerminalStateCheck()
    {
        var counter = new ObservationCounter();
        var terminator = new NeverTerminalStateTerminator().CountTerminatorCalls(counter);
        terminator.Counter.ShouldBeSameAs(counter);
        var instance = terminator.CreateExecutionInstance();
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
        var terminator = new NeverTerminalStateTerminator().MeasureTerminatorDuration(duration, timeProvider);
        terminator.Duration.ShouldBeSameAs(duration);
        terminator.TimeProvider.ShouldBeSameAs(timeProvider);
        var instance = terminator.CreateExecutionInstance();
        var problem = CreateProblem();

        instance.IsTerminalState(new CounterState { Value = 1 }, problem.SearchSpace, problem);
        instance.IsTerminalState(new CounterState { Value = 2 }, problem.SearchSpace, problem);

        duration.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(6));
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

    private sealed record SumParentsCrossover
      : StatelessCrossover<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>
    {
        public override IReadOnlyList<int> Cross(
            IReadOnlyList<IParents<int>> parents,
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

    private sealed record AddOneInterceptor
      : StatelessInterceptor<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, CounterState>
    {
        public override CounterState Transform(
            CounterState currentState,
            CounterState? previousState,
            DummySearchSpace<int> searchSpace,
            FuncProblem<int, DummySearchSpace<int>> problem)
        {
            return currentState with { Value = currentState.Value + 1 };
        }
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
