using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives;
using HEAL.HeuristicLib.Random;
using UniformDistributedCreator = HEAL.HeuristicLib.Encodings.RealVectors.UniformDistributedCreator;

namespace HEAL.HeuristicLib.Tests.ApiUsageSpecs.Algorithms.MetaAlgorithms;

public class CompositionSpecs
{
    [Fact]
    public async Task PipelineAlgorithm_CompositionExample_RunsTwoStages()
    {
        var problem = CreateRastriginProblem(dimension: 4);

        var firstStage = CreateSimpleHillClimber(problem, batchSize: 4, maxNeighbors: 8).WithMaxIterations(2);
        var secondStage = CreateSimpleHillClimber(problem, batchSize: 6, maxNeighbors: 10).WithMaxIterations(3);
        var pipeline = firstStage.Then(secondStage);

        var finalState = await pipeline.CompleteAsync(
          problem,
          RandomNumberGenerator.Create(111),
          ct: TestContext.Current.CancellationToken);

        problem.SearchSpace.Contains(finalState.EvaluatedCandidate.Candidate).ShouldBeTrue();
    }

    [Fact]
    public async Task CycleAlgorithm_CompositionExample_RunsFiniteCycles()
    {
        var problem = CreateRastriginProblem(dimension: 4);

        var firstStage = CreateSimpleHillClimber(problem, batchSize: 4, maxNeighbors: 8).WithMaxIterations(2);
        var secondStage = CreateSimpleHillClimber(problem, batchSize: 6, maxNeighbors: 10).WithMaxIterations(2);
        var cycle = firstStage.CycleWith(secondStage, maximumCycles: 2);

        var finalState = await cycle.CompleteAsync(
          problem,
          RandomNumberGenerator.Create(222),
          ct: TestContext.Current.CancellationToken);

        problem.SearchSpace.Contains(finalState.EvaluatedCandidate.Candidate).ShouldBeTrue();
    }

    private static TestFunctionProblem CreateRastriginProblem(int dimension)
    {
        return new TestFunctionProblem(new RastriginFunction(dimension));
    }

    private static HillClimber<RealVector> CreateSimpleHillClimber(
      TestFunctionProblem problem,
      int batchSize,
      int maxNeighbors)
    {
        return new HillClimber<RealVector>
        {
            Creator = new UniformDistributedCreator(problem.SearchSpace),
            Mutator = new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.15),
            Direction = LocalSearchDirection.FirstImprovement,
            BatchSize = batchSize,
            MaxNeighbors = maxNeighbors
        };
    }
}
