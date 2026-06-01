using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.Evolutionary;
using HEAL.HeuristicLib.Algorithms.MetaAlgorithms;
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
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Algorithms.MetaAlgorithms;

public class CycleAlgorithmTests
{
    [Fact]
    public void CycleAlgorithm_RunStreaming_RepeatsStagesAndPassesStateAcrossCycles()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var cycle = new CycleAlgorithm<AdditiveStepAlgorithm, int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>(
            [
                new AdditiveStepAlgorithm(1),
                new AdditiveStepAlgorithm(10)
            ])
        {
            MaximumCycles = 2
        };

        var states = cycle.RunStreaming(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).ToList();

        states.Select(MetaAlgorithmTestHelpers.StateGenotype).ShouldBe([1, 11, 12, 22]);
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
        var cycle = new CycleAlgorithm<GeneticAlgorithm<RealVector, RealVectorSearchSpace, TestFunctionProblem>, RealVector, RealVectorSearchSpace, TestFunctionProblem, PopulationState<RealVector>>([ga])
        {
            MaximumCycles = 5
        };

        var states = cycle.WithMaxIterations(8)
          .RunStreaming(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken)
          .ToList();

        states.Count.ShouldBe(8);
        states.Select(GetStateStamp).ShouldBe([1.0, 2.0, 3.0, 1.0, 2.0, 3.0, 1.0, 2.0]);
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
            Selector = new RandomSelector<RealVector>(),
            Elites = 0,
            Interceptor = new YieldedStateStampingInterceptor()
        };
    }

    private static double GetStateStamp(PopulationState<RealVector> state) =>
      state.Population.Solutions[0].ObjectiveVector[0];

    private sealed record YieldedStateStampingInterceptor
      : Interceptor<RealVector, RealVectorSearchSpace, TestFunctionProblem, PopulationState<RealVector>, YieldedStateStampingInterceptor.ExecutionState>
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
            var stampedSolutions = currentState.Population.Solutions
              .Select(solution => Solution.From(solution.Genotype, objectiveVector));

            return currentState with
            {
                Population = Population.From(stampedSolutions)
            };
        }

        public sealed class ExecutionState
        {
            public int YieldedStateCount { get; set; }
        }
    }
}
