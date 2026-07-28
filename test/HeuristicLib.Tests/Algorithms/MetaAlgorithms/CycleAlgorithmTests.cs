using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.Evolutionary;
using HEAL.HeuristicLib.Algorithms.MetaAlgorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators.Creators.RealVectorCreators;
using HEAL.HeuristicLib.Operators.Crossovers.RealVectorCrossovers;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Operators.Mutators.RealVectorMutators;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;
using HEAL.HeuristicLib.States;
using HEAL.HeuristicLib.Tests.TestSupport.Execution;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Algorithms.MetaAlgorithms;

public class CycleAlgorithmTests
{
    [Fact]
    public void CycleAlgorithm_RequiresAtLeastOneAlgorithm()
    {
        var exception = Should.Throw<ArgumentException>(() =>
            new CycleAlgorithm<AdditiveStepAlgorithm, int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>([]));

        exception.ParamName.ShouldBe("algorithms");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CycleAlgorithm_RequiresPositiveMaximumCycles(int maximumCycles)
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            CycleAlgorithm.Create(new AdditiveStepAlgorithm(1)) with
            {
                MaximumCycles = maximumCycles
            });
    }

    [Fact]
    public void CycleAlgorithm_RetriesCyclesWithoutProgressUntilMaximumCycles()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var algorithm = new NoProgressAlgorithm();
        var cycle = CycleAlgorithm.Create(algorithm) with
        {
            MaximumCycles = 3
        };

        var states = cycle.Stream(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).ToList();

        states.ShouldBeEmpty();
        algorithm.InstanceCount.ShouldBe(3);
    }

    [Fact]
    public void CycleAlgorithm_ContinuesWhenALaterChildProducesProgress()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        IAlgorithm<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>> firstAlgorithm = new NoProgressAlgorithm();
        var cycle = firstAlgorithm.CycleWith(new AdditiveStepAlgorithm(1), maximumCycles: 2);

        var states = cycle.Stream(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).ToList();

        states.Select(MetaAlgorithmTestHelpers.StateCandidate).ShouldBe([1, 2]);
    }

    [Fact]
    public void CycleAlgorithm_ChecksCancellationBeforeCreatingAChildInstance()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var evaluator = new CountingResolutionEvaluator();
        var algorithm = new CountingInstanceAlgorithm(1, evaluator);
        var cycle = CycleAlgorithm.Create(algorithm);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Should.Throw<OperationCanceledException>(() => cycle.Stream(problem, RandomNumberGenerator.Create(42), ct: cts.Token).ToList());

        algorithm.InstanceCount.ShouldBe(0);
    }

    [Fact]
    public void CycleAlgorithm_Stream_RepeatsStagesAndPassesStateAcrossCycles()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var cycle = new AdditiveStepAlgorithm(1).CycleWith(new AdditiveStepAlgorithm(10), maximumCycles: 2);

        var states = cycle.Stream(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).ToList();

        states.Select(MetaAlgorithmTestHelpers.StateCandidate).ShouldBe([1, 11, 12, 22]);
        states.Select(MetaAlgorithmTestHelpers.StateObjective).ShouldBe([1.0, 11.0, 12.0, 22.0]);
    }

    [Fact]
    public void CycleAlgorithm_WithInnerGeneticAlgorithmBudgetAndExternalEarlyStopping_YieldsPartialThirdCycle()
    {
        var problem = new TestFunctionProblem(new SphereFunction(dimension: 2));
        var ga = CreateStampedGeneticAlgorithm(problem) with
        {
            MaximumGenerations = 3
        };
        var cycle = CycleAlgorithm.Create(ga) with
        {
            MaximumCycles = 5
        };

        var states = cycle.WithMaxIterations(8)
          .Stream(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken)
          .ToList();

        states.Count.ShouldBe(8);
        states.Select(GetStateStamp).ShouldBe([1.0, 2.0, 3.0, 1.0, 2.0, 3.0, 1.0, 2.0]);
    }

    [Theory]
    [InlineData(true, 2)]
    [InlineData(false, 1)]
    public void CycleAlgorithm_UsesConfiguredAlgorithmInstanceLifecycleWhileReusingResolvedParentDependencies(bool newExecutionInstancesPerCycle, int expectedAlgorithmInstances)
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var evaluator = new CountingResolutionEvaluator();
        var algorithm = new CountingInstanceAlgorithm(1, evaluator);
        var cycle = CycleAlgorithm.Create(algorithm) with
        {
            MaximumCycles = 2,
            NewExecutionInstancesPerCycle = newExecutionInstancesPerCycle
        };
        var registry = new ExecutionInstanceRegistry();
        _ = registry.Resolve(evaluator);
        var cycleInstance = registry.Resolve(cycle);

        var states = cycleInstance.Stream(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).ToList();

        states.Select(MetaAlgorithmTestHelpers.StateCandidate).ShouldBe([1, 2]);
        algorithm.InstanceCount.ShouldBe(expectedAlgorithmInstances);
        evaluator.InstanceCount.ShouldBe(1);
    }

    private static GeneticAlgorithm<RealVector, RealVectorSearchSpace, TestFunctionProblem> CreateStampedGeneticAlgorithm(
      TestFunctionProblem problem)
    {
        return new GeneticAlgorithm<RealVector, RealVectorSearchSpace, TestFunctionProblem>
        {
            PopulationSize = 4,
            Creator = new UniformDistributedCreator(problem.SearchSpace),
            Crossover = new SinglePointCrossover(),
            Mutator = new GaussianMutator(0.1, 0.1),
            MutationRate = 0.5,
            Selector = RandomSelector.For(problem),
            Elites = 0,
            Interceptor = new YieldedStateStampingInterceptor()
        };
    }

    private static double GetStateStamp(PopulationState<RealVector> state) =>
      state.Population.EvaluatedCandidates[0].ObjectiveVector[0];

    private sealed record NoProgressAlgorithm
        : Algorithm<NoProgressAlgorithm, int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>
    {
        public int InstanceCount { get; private set; }

        protected override AlgorithmInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>> CreateAlgorithmInstance(ExecutionInstanceRegistry registry)
        {
            InstanceCount++;
            return new Instance();
        }

        private sealed class Instance : AlgorithmInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>
        {
            public override async IAsyncEnumerable<PopulationState<int>> RunStreamingAsync(IProblem<int, DummySearchSpace<int>> problem, IRandomNumberGenerator random, PopulationState<int>? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
            {
                await Task.CompletedTask;
                yield break;
            }
        }
    }

    private sealed record YieldedStateStampingInterceptor
      : StatefulInterceptor<RealVector, RealVectorSearchSpace, TestFunctionProblem, PopulationState<RealVector>, YieldedStateStampingInterceptor.ExecutionState>
    {
        protected override ExecutionState CreateInitialState() => new();

        protected override PopulationState<RealVector> Transform(
          PopulationState<RealVector> currentState,
          PopulationState<RealVector>? previousState,
          ExecutionState executionState,
          RealVectorSearchSpace searchSpace,
          TestFunctionProblem problem)
        {
            executionState.YieldedStateCount++;
            var objectiveVector = new ObjectiveVector(executionState.YieldedStateCount);
            var stampedCandidates = currentState.Population.EvaluatedCandidates
                            .Select(solution => EvaluatedCandidate.From(solution.Candidate, objectiveVector));

            return currentState with
            {
                Population = Population.From(stampedCandidates)
            };
        }

        public sealed class ExecutionState
        {
            public int YieldedStateCount { get; set; }
        }
    }
}
