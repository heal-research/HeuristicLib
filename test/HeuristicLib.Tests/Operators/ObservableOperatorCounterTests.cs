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
using HEAL.HeuristicLib.Tests.TestSupport.Execution;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Operators;

public class ObservableOperatorCounterTests
{
    [Fact]
    public void CountCreatorCalls_IncrementsOncePerCreateCall()
    {
        var counter = new ObservationCounter();
        var creator = new SequenceCreator().CountCreatorCalls(counter);
        var instance = creator.CreateExecutionInstance(TestRun.Instance);
        var problem = CreateProblem();

        instance.Create(3, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        instance.Create(1, RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        counter.CurrentCount.ShouldBe(2);
    }

    [Fact]
    public void CountCreatedGenotypes_IncrementsByReturnedGenotypeCount()
    {
        var counter = new ObservationCounter();
        var creator = new SequenceCreator().CountCreatedGenotypes(counter);
        var instance = creator.CreateExecutionInstance(TestRun.Instance);
        var problem = CreateProblem();

        instance.Create(3, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        instance.Create(1, RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        counter.CurrentCount.ShouldBe(4);
    }

    [Fact]
    public void CountMutatorCalls_IncrementsOncePerMutateCall()
    {
        var counter = new ObservationCounter();
        var mutator = new AddOneMutator().CountMutatorCalls(counter);
        var instance = mutator.CreateExecutionInstance(TestRun.Instance);
        var problem = CreateProblem();

        instance.Mutate([1, 2, 3], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        instance.Mutate([4], RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        counter.CurrentCount.ShouldBe(2);
    }

    [Fact]
    public void CountMutatedGenotypes_IncrementsByReturnedGenotypeCount()
    {
        var counter = new ObservationCounter();
        var mutator = new AddOneMutator().CountMutatedGenotypes(counter);
        var instance = mutator.CreateExecutionInstance(TestRun.Instance);
        var problem = CreateProblem();

        instance.Mutate([1, 2, 3], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        instance.Mutate([4], RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        counter.CurrentCount.ShouldBe(4);
    }

    [Fact]
    public void CountCrossoverCalls_IncrementsOncePerCrossCall()
    {
        var counter = new ObservationCounter();
        var crossover = new SumParentsCrossover().CountCrossoverCalls(counter);
        var instance = crossover.CreateExecutionInstance(TestRun.Instance);
        var problem = CreateProblem();

        instance.Cross(
            [new Parents<int>(1, 10), new Parents<int>(2, 20), new Parents<int>(3, 30)],
            RandomNumberGenerator.Create(1),
            problem.SearchSpace,
            problem);
        instance.Cross(
            [new Parents<int>(4, 40)],
            RandomNumberGenerator.Create(2),
            problem.SearchSpace,
            problem);

        counter.CurrentCount.ShouldBe(2);
    }

    [Fact]
    public void CountCrossedGenotypes_IncrementsByReturnedGenotypeCount()
    {
        var counter = new ObservationCounter();
        var crossover = new SumParentsCrossover().CountCrossedGenotypes(counter);
        var instance = crossover.CreateExecutionInstance(TestRun.Instance);
        var problem = CreateProblem();

        instance.Cross(
            [new Parents<int>(1, 10), new Parents<int>(2, 20), new Parents<int>(3, 30)],
            RandomNumberGenerator.Create(1),
            problem.SearchSpace,
            problem);
        instance.Cross(
            [new Parents<int>(4, 40)],
            RandomNumberGenerator.Create(2),
            problem.SearchSpace,
            problem);

        counter.CurrentCount.ShouldBe(4);
    }

    [Fact]
    public void CountSelectorCalls_IncrementsOncePerSelectCall()
    {
        var counter = new ObservationCounter();
        var selector = new FirstSolutionsSelector().CountSelectorCalls(counter);
        var instance = selector.CreateExecutionInstance(TestRun.Instance);
        var problem = CreateProblem();

        instance.Select(CreateSolutions([1, 2, 3]), problem.Objective, 2, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        instance.Select(CreateSolutions([4]), problem.Objective, 1, RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        counter.CurrentCount.ShouldBe(2);
    }

    [Fact]
    public void CountSelectedSolutions_IncrementsByReturnedSolutionCount()
    {
        var counter = new ObservationCounter();
        var selector = new FirstSolutionsSelector().CountSelectedSolutions(counter);
        var instance = selector.CreateExecutionInstance(TestRun.Instance);
        var problem = CreateProblem();

        instance.Select(CreateSolutions([1, 2, 3]), problem.Objective, 2, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        instance.Select(CreateSolutions([4]), problem.Objective, 1, RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        counter.CurrentCount.ShouldBe(3);
    }

    [Fact]
    public void CountReplacerCalls_IncrementsOncePerReplaceCall()
    {
        var counter = new ObservationCounter();
        var replacer = new FirstReplacementSolutionsReplacer().CountReplacerCalls(counter);
        var instance = replacer.CreateExecutionInstance(TestRun.Instance);
        var problem = CreateProblem();

        instance.Replace(
            CreateSolutions([1, 2, 3]),
            CreateSolutions([10, 20]),
            problem.Objective,
            2,
            RandomNumberGenerator.Create(1),
            problem.SearchSpace,
            problem);
        instance.Replace(
            CreateSolutions([4]),
            CreateSolutions([40]),
            problem.Objective,
            1,
            RandomNumberGenerator.Create(2),
            problem.SearchSpace,
            problem);

        counter.CurrentCount.ShouldBe(2);
    }

    [Fact]
    public void CountReplacementSolutions_IncrementsByReturnedSolutionCount()
    {
        var counter = new ObservationCounter();
        var replacer = new FirstReplacementSolutionsReplacer().CountReplacementSolutions(counter);
        var instance = replacer.CreateExecutionInstance(TestRun.Instance);
        var problem = CreateProblem();

        instance.Replace(
            CreateSolutions([1, 2, 3]),
            CreateSolutions([10, 20]),
            problem.Objective,
            2,
            RandomNumberGenerator.Create(1),
            problem.SearchSpace,
            problem);
        instance.Replace(
            CreateSolutions([4]),
            CreateSolutions([40]),
            problem.Objective,
            1,
            RandomNumberGenerator.Create(2),
            problem.SearchSpace,
            problem);

        counter.CurrentCount.ShouldBe(3);
    }

    [Fact]
    public void CountInterceptorCalls_IncrementsOncePerTransformCall()
    {
        var counter = new ObservationCounter();
        var interceptor = new AddOneInterceptor().CountInterceptorCalls(counter);
        var instance = interceptor.CreateExecutionInstance(TestRun.Instance);
        var problem = CreateProblem();

        instance.Transform(new CounterState { Value = 1 }, previousState: null, problem.SearchSpace, problem);
        instance.Transform(new CounterState { Value = 2 }, new CounterState { Value = 1 }, problem.SearchSpace, problem);

        counter.CurrentCount.ShouldBe(2);
    }

    [Fact]
    public void CountTerminatorCalls_IncrementsOncePerTerminalStateCheck()
    {
        var counter = new ObservationCounter();
        var terminator = new NeverTerminalStateTerminator().CountTerminatorCalls(counter);
        var instance = terminator.CreateExecutionInstance(TestRun.Instance);
        var problem = CreateProblem();

        instance.IsTerminalState(new CounterState { Value = 1 }, problem.SearchSpace, problem);
        instance.IsTerminalState(new CounterState { Value = 2 }, problem.SearchSpace, problem);

        counter.CurrentCount.ShouldBe(2);
    }

    private static FuncProblem<int, DummySearchSpace<int>> CreateProblem()
    {
        return FuncProblem.Create<int, DummySearchSpace<int>>(
            evaluateFunc: static genotype => genotype,
            encoding: DummySearchSpace<int>.Instance,
            objective: SingleObjective.Minimize);
    }

    private static IReadOnlyList<ISolution<int>> CreateSolutions(IReadOnlyList<int> genotypes)
    {
        return genotypes
            .Select(genotype => new Solution<int>(genotype, new ObjectiveVector(genotype)))
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

    private sealed record FirstSolutionsSelector
      : StatelessSelector<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>
    {
        public override IReadOnlyList<ISolution<int>> Select(
            IReadOnlyList<ISolution<int>> population,
            Objective objective,
            int count,
            IRandomNumberGenerator random,
            DummySearchSpace<int> searchSpace,
            FuncProblem<int, DummySearchSpace<int>> problem)
        {
            return population.Take(count).ToArray();
        }
    }

    private sealed record FirstReplacementSolutionsReplacer
      : StatelessReplacer<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>
    {
        public override IReadOnlyList<ISolution<int>> Replace(
            IReadOnlyList<ISolution<int>> previousPopulation,
            IReadOnlyList<ISolution<int>> offspringPopulation,
            Objective objective,
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
}
