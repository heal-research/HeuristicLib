using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Problems.TestFunctions.ZDT;
using SinglePointCrossover = HEAL.HeuristicLib.Encodings.RealVectors.SinglePointCrossover;
using UniformDistributedCreator = HEAL.HeuristicLib.Encodings.RealVectors.UniformDistributedCreator;

namespace HEAL.HeuristicLib.Tests.Algorithms.Evolutionary;

public class NSGA2Tests
{
    [Fact]
    public void Complete_ReturnsMultiObjectivePopulationWithinProblemSearchSpace()
    {
        var problem = new MultiObjectiveTestFunctionProblem(new Zdt1(dimension: 3));
        var algorithm = new NSGA2<RealVector, RealVectorSearchSpace, MultiObjectiveTestFunctionProblem>
        {
            Creator = new UniformDistributedCreator(problem.SearchSpace),
            Crossover = new SinglePointCrossover(),
            Mutator = new GaussianMutator(0.1, 0.1),
            PopulationSize = 5,
            MutationRate = 0.5,
            MaximumGenerations = 5
        };

        var result = algorithm.Complete(
          problem,
          RandomNumberGenerator.Create(42),
          ct: TestContext.Current.CancellationToken);

        result.Population.EvaluatedCandidates.Count.ShouldBe(5);
        result.Population.EvaluatedCandidates.All(solution => problem.SearchSpace.Contains(solution.Candidate)).ShouldBeTrue();
        result.Population.EvaluatedCandidates.All(solution => solution.ObjectiveVector.Count == 2).ShouldBeTrue();
    }
}
