using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems.Dynamic;
using HEAL.HeuristicLib.Problems.Dynamic.Operators;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Tests.Algorithms;

public class DynamicGeneticAlgorithmTests
{
    [Fact]
    public void RandomImmigrantGeneticAlgorithm_AddsCreatorGeneratedImmigrantsToReplacementPool()
    {
        var problem = new IntegerDynamicProblem(epochLength: 10_000);
        var algorithm = new RandomImmigrantGeneticAlgorithm<int, IntegerSearchSpace, IntegerDynamicProblem>
        {
            PopulationSize = 4,
            Creator = new InitialThenImmigrantCreator(),
            Crossover = new ConstantCrossover(1_000),
            Mutator = new OffsetMutator(0),
            MutationRate = 1.0,
            ImmigrantRate = 0.5,
            Selector = new RepeatSelector(),
            Evaluator = new ProblemEvaluator<int>(),
            Elites = 0,
            MaximumGenerations = 2
        };

        var states = algorithm.Stream(problem,
                                  RandomNumberGenerator.Create(1),
                                  ct: TestContext.Current.CancellationToken)
                              .ToList();

        states.Count.ShouldBe(2);
        states[1].Population.Select(x => x.Candidate).ShouldContain(0);
        states[1].Population.Select(x => x.Candidate).ShouldContain(1);
    }

    [Fact]
    public void RandomImmigrantGeneticAlgorithm_CapsImmigrantsToNonEliteSlots()
    {
        var problem = new IntegerDynamicProblem(epochLength: 10_000);
        var algorithm = new RandomImmigrantGeneticAlgorithm<int, IntegerSearchSpace, IntegerDynamicProblem>
        {
            PopulationSize = 4,
            Creator = new InitialThenImmigrantCreator(),
            Crossover = new ConstantCrossover(1_000),
            Mutator = new OffsetMutator(0),
            MutationRate = 1.0,
            ImmigrantRate = 1.0,
            Selector = new RepeatSelector(),
            Evaluator = new ProblemEvaluator<int>(),
            Elites = 3,
            MaximumGenerations = 2
        };

        var states = algorithm.Stream(problem,
                                  RandomNumberGenerator.Create(1),
                                  ct: TestContext.Current.CancellationToken)
                              .ToList();

        var secondGeneration = states[1].Population.Select(x => x.Candidate).ToArray();
        secondGeneration.ShouldContain(0);
        secondGeneration.ShouldNotContain(1);
        secondGeneration.ShouldContain(100);
        secondGeneration.ShouldContain(101);
        secondGeneration.ShouldContain(102);
    }

    [Fact]
    public void RandomImmigrantGeneticAlgorithm_ZeroImmigrantRateDoesNotCreateImmigrants()
    {
        var problem = new IntegerDynamicProblem(epochLength: 10_000);
        var algorithm = new RandomImmigrantGeneticAlgorithm<int, IntegerSearchSpace, IntegerDynamicProblem>
        {
            PopulationSize = 4,
            Creator = new InitialThenImmigrantCreator(),
            Crossover = new ConstantCrossover(1_000),
            Mutator = new OffsetMutator(0),
            MutationRate = 1.0,
            ImmigrantRate = 0.0,
            Selector = new RepeatSelector(),
            Evaluator = new ProblemEvaluator<int>(),
            Elites = 0,
            MaximumGenerations = 2
        };

        var states = algorithm.Stream(problem,
                                  RandomNumberGenerator.Create(1),
                                  ct: TestContext.Current.CancellationToken)
                              .ToList();

        states[1].Population.Select(x => x.Candidate).ShouldAllBe(candidate => candidate == 1_000);
    }

    [Fact]
    public void RandomImmigrantGeneticAlgorithm_UsesDedicatedImmigrantCreatorWhenConfigured()
    {
        var problem = new IntegerDynamicProblem(epochLength: 10_000);
        var algorithm = new RandomImmigrantGeneticAlgorithm<int, IntegerSearchSpace, IntegerDynamicProblem>
        {
            PopulationSize = 4,
            Creator = new ConstantCreator(100),
            ImmigrantCreator = new ConstantCreator(7),
            Crossover = new ConstantCrossover(1_000),
            Mutator = new OffsetMutator(0),
            MutationRate = 1.0,
            ImmigrantRate = 0.5,
            Selector = new RepeatSelector(),
            Evaluator = new ProblemEvaluator<int>(),
            Elites = 0,
            MaximumGenerations = 2
        };

        var states = algorithm.Stream(problem,
                                  RandomNumberGenerator.Create(1),
                                  ct: TestContext.Current.CancellationToken)
                              .ToList();

        var secondGeneration = states[1].Population.Select(x => x.Candidate).ToArray();
        secondGeneration.Count(candidate => candidate == 7).ShouldBe(2);
        secondGeneration.Count(candidate => candidate == 1_000).ShouldBe(2);
    }

    [Fact]
    public void RandomImmigrantGeneticAlgorithm_RespectsImmigrantInterval()
    {
        var problem = new IntegerDynamicProblem(epochLength: 10_000);
        var algorithm = new RandomImmigrantGeneticAlgorithm<int, IntegerSearchSpace, IntegerDynamicProblem>
        {
            PopulationSize = 4,
            Creator = new ConstantCreator(100),
            ImmigrantCreator = new ConstantCreator(7),
            Crossover = new ConstantCrossover(1_000),
            Mutator = new OffsetMutator(0),
            MutationRate = 1.0,
            ImmigrantRate = 0.5,
            ImmigrantInterval = 2,
            Selector = new RepeatSelector(),
            Evaluator = new ProblemEvaluator<int>(),
            Elites = 0,
            MaximumGenerations = 3
        };

        var states = algorithm.Stream(problem,
                                  RandomNumberGenerator.Create(1),
                                  ct: TestContext.Current.CancellationToken)
                              .ToList();

        states[1].Population.Select(x => x.Candidate).ShouldAllBe(candidate => candidate == 1_000);

        var secondGeneratedGeneration = states[2].Population.Select(x => x.Candidate).ToArray();
        secondGeneratedGeneration.Count(candidate => candidate == 7).ShouldBe(2);
        secondGeneratedGeneration.Count(candidate => candidate == 1_000).ShouldBe(2);
    }

    [Fact]
    public void ChangeTriggeredRandomImmigrantGeneticAlgorithm_InjectsOnlyAfterObservedEpochChange()
    {
        var problem = new IntegerDynamicProblem(epochLength: 10_000);
        var algorithm = new ChangeTriggeredRandomImmigrantGeneticAlgorithm<int, IntegerSearchSpace, IntegerDynamicProblem>
        {
            PopulationSize = 4,
            Creator = new ConstantCreator(100),
            ImmigrantCreator = new ConstantCreator(7),
            Crossover = new ConstantCrossover(1_000),
            Mutator = new OffsetMutator(0),
            MutationRate = 1.0,
            ImmigrantRate = 0.5,
            Selector = new RepeatSelector(),
            Evaluator = new ProblemEvaluator<int>(),
            Elites = 0
        };

        using var enumerator = algorithm.Stream(problem,
                                            RandomNumberGenerator.Create(1),
                                            ct: TestContext.Current.CancellationToken)
                                        .GetEnumerator();

        enumerator.MoveNext().ShouldBeTrue();
        enumerator.Current.Population.Select(x => x.Candidate).ShouldAllBe(candidate => candidate == 100);

        enumerator.MoveNext().ShouldBeTrue();
        enumerator.Current.Population.Select(x => x.Candidate).ShouldAllBe(candidate => candidate == 1_000);

        problem.UpdateOnce();

        enumerator.MoveNext().ShouldBeTrue();
        var afterEpochChange = enumerator.Current.Population.Select(x => x.Candidate).ToArray();
        afterEpochChange.Count(candidate => candidate == 7).ShouldBe(2);
        afterEpochChange.Count(candidate => candidate == 1_000).ShouldBe(2);

        enumerator.MoveNext().ShouldBeTrue();
        enumerator.Current.Population.Select(x => x.Candidate).ShouldAllBe(candidate => candidate == 1_000);
    }

    [Fact]
    public void DynamicRateMutator_UsesResponseRateAfterEpochChangeThenFallsBackToBaseRate()
    {
        var problem = new IntegerDynamicProblem(epochLength: 10_000);
        var mutator = new OffsetMutator(10).WithDynamicRate(problem, baseRate: 0.0, responseRate: 1.0, responseDuration: 2);
        var instance = ResolutionScope.Create().Resolve<int, IntegerSearchSpace, IntegerDynamicProblem>(mutator);
        var random = new CountingZeroRandomNumberGenerator();

        instance.Mutate([1, 2], random, problem.SearchSpace, problem).ShouldBe([1, 2]);

        problem.UpdateOnce();

        instance.Mutate([1, 2], random, problem.SearchSpace, problem).ShouldBe([11, 12]);
        instance.Mutate([1, 2], random, problem.SearchSpace, problem).ShouldBe([11, 12]);
        instance.Mutate([1, 2], random, problem.SearchSpace, problem).ShouldBe([1, 2]);

        random.NextDoubleCalls.ShouldBe(0);
        random.NextIntCalls.ShouldBe(0);
    }

    [Fact]
    public void DynamicRateMutator_RebindingPreservesRemainingResponseCalls()
    {
        using var problem = new IntegerDynamicProblem(epochLength: 10_000);
        var mutator = new OffsetMutator(10).WithDynamicRate(problem, baseRate: 0.0, responseRate: 1.0, responseDuration: 2);
        var scope = ResolutionScope.Create();
        var execution = scope.Resolve<int, IntegerSearchSpace, IntegerDynamicProblem>(mutator);
        var random = new CountingZeroRandomNumberGenerator();
        execution.Mutate([1], random, problem.SearchSpace, problem).ShouldBe([1]);
        problem.UpdateOnce();
        execution.Mutate([1], random, problem.SearchSpace, problem).ShouldBe([11]);

        var rebound = scope.Resolve<int, IntegerSearchSpace, IntegerDynamicProblem>(mutator);
        rebound.Mutate([1], random, problem.SearchSpace, problem).ShouldBe([11]);
        rebound.Mutate([1], random, problem.SearchSpace, problem).ShouldBe([1]);
    }

    [Fact]
    public void ProblemEvaluationCount_PreservesEvaluationsAcrossEpochRestarts()
    {
        using var problem = new IntegerDynamicProblem(epochLength: 10_000);
        var random = new CountingZeroRandomNumberGenerator();
        _ = problem.Evaluate([1, 2], random);
        problem.UpdateOnce();
        _ = problem.Evaluate([3], random);

        problem.TotalEvaluations.ShouldBe(3);
        problem.CurrentEpoch.ShouldBe(1);
    }

    [Fact]
    public void DynamicRateMutator_ResponseDurationZeroIgnoresEpochChange()
    {
        var problem = new IntegerDynamicProblem(epochLength: 10_000);
        var mutator = new OffsetMutator(10).WithDynamicRate(problem, baseRate: 0.0, responseRate: 1.0, responseDuration: 0);
        var instance = ResolutionScope.Create().Resolve<int, IntegerSearchSpace, IntegerDynamicProblem>(mutator);
        var random = new CountingZeroRandomNumberGenerator();

        instance.Mutate([1, 2], random, problem.SearchSpace, problem).ShouldBe([1, 2]);

        problem.UpdateOnce();

        instance.Mutate([1, 2], random, problem.SearchSpace, problem).ShouldBe([1, 2]);
    }

    [Fact]
    public void DynamicRateMutator_ForkPreservesIndependentResponseState()
    {
        var problem = new IntegerDynamicProblem(epochLength: 10_000);
        var mutator = new OffsetMutator(10).WithDynamicRate(problem, baseRate: 0.0, responseRate: 1.0, responseDuration: 2);
        var sourceScope = ResolutionScope.Create();
        var source = sourceScope.Resolve<int, IntegerSearchSpace, IntegerDynamicProblem>(mutator);
        var random = new CountingZeroRandomNumberGenerator();

        source.Mutate([1], random, problem.SearchSpace, problem).ShouldBe([1]);
        problem.UpdateOnce();
        source.Mutate([1], random, problem.SearchSpace, problem).ShouldBe([11]);

        var fork = sourceScope.CreateForkedScope();
        var forked = fork.Resolve<int, IntegerSearchSpace, IntegerDynamicProblem>(mutator);
        var secondFork = fork.CreateForkedScope();
        var twiceForked = secondFork.Resolve<int, IntegerSearchSpace, IntegerDynamicProblem>(mutator);

        forked.ShouldNotBeSameAs(source);
        twiceForked.ShouldNotBeSameAs(forked);
        forked.Mutate([1], random, problem.SearchSpace, problem).ShouldBe([11]);
        forked.Mutate([1], random, problem.SearchSpace, problem).ShouldBe([1]);
        twiceForked.Mutate([1], random, problem.SearchSpace, problem).ShouldBe([11]);
        twiceForked.Mutate([1], random, problem.SearchSpace, problem).ShouldBe([1]);
        source.Mutate([1], random, problem.SearchSpace, problem).ShouldBe([11]);
        source.Mutate([1], random, problem.SearchSpace, problem).ShouldBe([1]);
    }

    [Fact]
    public void DynamicRateMutator_ForkRebuildsNonForkableChooseOneComposite()
    {
        var problem = new IntegerDynamicProblem(epochLength: 10_000);
        var child = ChooseOneMutator.Create<int>(
            new OffsetMutator(10));
        var mutator = child.WithDynamicRate(problem, baseRate: 0.0, responseRate: 1.0, responseDuration: 2);
        var sourceScope = ResolutionScope.Create();
        var source = sourceScope.Resolve<int, IntegerSearchSpace, IntegerDynamicProblem>(mutator);
        var random = new CountingZeroRandomNumberGenerator();

        source.Mutate([1], random, problem.SearchSpace, problem).ShouldBe([1]);
        problem.UpdateOnce();
        source.Mutate([1], random, problem.SearchSpace, problem).ShouldBe([11]);

        var fork = sourceScope.CreateForkedScope();
        var forked = fork.Resolve<int, IntegerSearchSpace, IntegerDynamicProblem>(mutator);

        forked.ShouldNotBeSameAs(source);
        forked.Mutate([1], random, problem.SearchSpace, problem).ShouldBe([11]);
        source.Mutate([1], random, problem.SearchSpace, problem).ShouldBe([11]);
    }

    [Fact]
    public void DynamicRateMutator_RejectsReuseWithDifferentProblemInstance()
    {
        var problem = new IntegerDynamicProblem(epochLength: 10_000);
        var otherProblem = new IntegerDynamicProblem(epochLength: 10_000);
        var mutator = new OffsetMutator(10).WithDynamicRate(problem, baseRate: 0.0, responseRate: 1.0, responseDuration: 1);
        var instance = ResolutionScope.Create().Resolve<int, IntegerSearchSpace, IntegerDynamicProblem>(mutator);
        var random = new CountingZeroRandomNumberGenerator();

        _ = instance.Mutate([1], random, problem.SearchSpace, problem);

        Should.Throw<InvalidOperationException>(() =>
            instance.Mutate([1], random, otherProblem.SearchSpace, otherProblem));
    }

    [Fact]
    public void HypermutationGeneticAlgorithm_Create_ReturnsOrdinaryGeneticAlgorithmWithDynamicRateMutator()
    {
        var algorithm = HypermutationGeneticAlgorithm.Create<int, IntegerSearchSpace, IntegerDynamicProblem>(
            new InitialThenImmigrantCreator(),
            new ConstantCrossover(1_000),
            new OffsetMutator(10),
            baseMutationRate: 0.1,
            hypermutationRate: 1.0,
            hypermutationDuration: 2,
            new RepeatSelector(),
            populationSize: 4,
            new ProblemEvaluator<int>());

        algorithm.ShouldBeOfType<GeneticAlgorithm<int>>();
        algorithm.MutationRate.ShouldBe(1.0);
        algorithm.Mutator.ShouldBeOfType<DynamicRateMutator<int, IntegerSearchSpace, IntegerDynamicProblem>>();
    }

    private sealed class IntegerSearchSpace : ISearchSpace<int>
    {
        public bool Contains(int candidate) => true;
    }

    private sealed class IntegerDynamicProblem : DynamicProblem<IntegerDynamicProblem, int, IntegerSearchSpace>
    {
        public IntegerDynamicProblem(int epochLength)
            : base(SingleObjective.Minimize, new IntegerSearchSpace(), RandomNumberGenerator.Create(0),
                new EvaluationCountSchedule(epochLength), UpdatePolicy.AfterEachEvaluation)
        { }

        protected override ObjectiveVector Evaluate(int candidate, IRandomNumberGenerator random, int epoch) => candidate;

        protected override void Update()
        { }
    }

    private sealed record InitialThenImmigrantCreator
        : StatefulCreator<int, IntegerSearchSpace, IntegerDynamicProblem, InitialThenImmigrantCreator.State>
    {
        protected override State CreateInitialState() => new();

        protected override IReadOnlyList<int> Create(int count, State state, IRandomNumberGenerator random,
                                                     IntegerSearchSpace searchSpace, IntegerDynamicProblem problem)
        {
            var start = state.CallCount++ == 0 ? 100 : 0;
            return Enumerable.Range(start, count).ToArray();
        }

        public sealed class State
        {
            public int CallCount { get; set; }
        }
    }

    private sealed record ConstantCreator(int Value)
        : StatelessCreator<int, IntegerSearchSpace, IntegerDynamicProblem>
    {
        public override IReadOnlyList<int> Create(int count, IRandomNumberGenerator random,
                                                  IntegerSearchSpace searchSpace, IntegerDynamicProblem problem) =>
            Enumerable.Repeat(Value, count).ToArray();
    }

    private sealed record ConstantCrossover(int Value)
        : StatelessCrossover<int, IntegerSearchSpace, IntegerDynamicProblem>
    {
        public override IReadOnlyList<int> Cross(IReadOnlyList<Parents<int>> parents, IRandomNumberGenerator random,
                                                 IntegerSearchSpace searchSpace, IntegerDynamicProblem problem) =>
            Enumerable.Repeat(Value, parents.Count).ToArray();
    }

    private sealed record OffsetMutator(int Offset) : StatelessMutator<int, IntegerSearchSpace, IntegerDynamicProblem>
    {
        public override IReadOnlyList<int> Mutate(IReadOnlyList<int> parents, IRandomNumberGenerator random,
                                                  IntegerSearchSpace searchSpace, IntegerDynamicProblem problem) =>
            parents.Select(parent => parent + Offset).ToArray();
    }

    private sealed record RepeatSelector : StatelessSelector<int, IntegerSearchSpace, IntegerDynamicProblem>
    {
        public override IReadOnlyList<EvaluatedCandidate<int>> Select(
            IReadOnlyList<EvaluatedCandidate<int>> population,
            ObjectiveDirections objective,
            int count,
            IRandomNumberGenerator random,
            IntegerSearchSpace searchSpace,
            IntegerDynamicProblem problem)
        {
            return Enumerable.Range(0, count)
                             .Select(index => population[index % population.Count])
                             .ToArray();
        }
    }

    private sealed class CountingZeroRandomNumberGenerator : IRandomNumberGenerator
    {
        public int NextDoubleCalls { get; private set; }
        public int NextIntCalls { get; private set; }

        public double NextDouble()
        {
            NextDoubleCalls++;
            return 0;
        }

        public int NextInt()
        {
            NextIntCalls++;
            return 0;
        }

        public IRandomNumberGenerator Fork(ulong forkKey) => this;
    }
}
