using System.Collections.Immutable;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.LocalSearch;
using HEAL.HeuristicLib.Algorithms.MetaAlgorithms;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators.Creators.RealVectorCreators;
using HEAL.HeuristicLib.Operators.Mutators.RealVectorMutators;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;
using HEAL.HeuristicLib.States;
using Xunit;

namespace HEAL.HeuristicLib.ApiSpecs;

public class CompositionSpecs
{
  [Fact(Explicit = true)]
  public async Task PipelineAlgorithm_CompositionExample_RunsTwoStages()
  {
    var problem = CreateRastriginProblem(dimension: 4);

    IAlgorithm<RealVector, RealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>[] stages = [
      CreateSimpleHillClimber(problem, batchSize: 4, maxNeighbors: 8).WithMaxIterations(2),
      CreateSimpleHillClimber(problem, batchSize: 6, maxNeighbors: 10).WithMaxIterations(3)
    ];

    var pipeline = new PipelineAlgorithm<
      IAlgorithm<RealVector, RealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>,
      RealVector,
      RealVectorSearchSpace,
      TestFunctionProblem,
      SingleSolutionState<RealVector>>(ImmutableArray.Create(stages));

    var finalState = await pipeline.RunToCompletionAsync(
      problem,
      RandomNumberGenerator.Create(111),
      ct: TestContext.Current.CancellationToken);

    problem.SearchSpace.Contains(finalState.Solution.Genotype).ShouldBeTrue();
  }

  [Fact(Explicit = true)]
  public async Task CycleAlgorithm_CompositionExample_RunsFiniteCycles()
  {
    var problem = CreateRastriginProblem(dimension: 4);

    IAlgorithm<RealVector, RealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>[] stages = [
      CreateSimpleHillClimber(problem, batchSize: 4, maxNeighbors: 8).WithMaxIterations(2),
      CreateSimpleHillClimber(problem, batchSize: 6, maxNeighbors: 10).WithMaxIterations(2)
    ];

    var cycle = new CycleAlgorithm<
      IAlgorithm<RealVector, RealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>,
      RealVector,
      RealVectorSearchSpace,
      TestFunctionProblem,
      SingleSolutionState<RealVector>>(ImmutableArray.Create(stages)) {
      MaximumCycles = 2,
      NewExecutionInstancesPerCycle = true
    };

    var finalState = await cycle.RunToCompletionAsync(
      problem,
      RandomNumberGenerator.Create(222),
      ct: TestContext.Current.CancellationToken);

    problem.SearchSpace.Contains(finalState.Solution.Genotype).ShouldBeTrue();
  }

  private static TestFunctionProblem CreateRastriginProblem(int dimension)
  {
    return new TestFunctionProblem(new RastriginFunction(dimension));
  }

  private static HillClimber<RealVector, RealVectorSearchSpace, TestFunctionProblem> CreateSimpleHillClimber(
    TestFunctionProblem problem,
    int batchSize,
    int maxNeighbors)
  {
    return new HillClimber<RealVector, RealVectorSearchSpace, TestFunctionProblem> {
      Creator = new UniformDistributedCreator(problem.SearchSpace),
      Mutator = new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.15),
      Direction = LocalSearchDirection.FirstImprovement,
      BatchSize = batchSize,
      MaxNeighbors = maxNeighbors
    };
  }
}
