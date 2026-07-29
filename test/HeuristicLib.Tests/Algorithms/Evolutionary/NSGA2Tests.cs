using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.Evolutionary;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators.Creators.RealVectorCreators;
using HEAL.HeuristicLib.Operators.Crossovers.RealVectorCrossovers;
using HEAL.HeuristicLib.Operators.Mutators.RealVectorMutators;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Problems.TestFunctions.ZDT;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Tests.Algorithms.Evolutionary;

public class NSGA2Tests
{
    [Fact]
    public void Complete_ReturnsMultiObjectivePopulationWithinProblemSearchSpace()
    {
        var problem = new MultiObjectiveTestFunctionProblem(new Zdt1(dimension: 3));
        var algorithm = NSGA2.GetBuilder<RealVector, RealVectorSearchSpace, MultiObjectiveTestFunctionProblem>(
          new UniformDistributedCreator(problem.SearchSpace),
          new SinglePointCrossover(),
          new GaussianMutator(0.1, 0.1));
        algorithm.PopulationSize = 5;
        algorithm.MutationRate = 0.5;

        var result = (algorithm.Build() with
        {
            MaximumGenerations = 5
        }).Complete(
          problem,
          RandomNumberGenerator.Create(42),
          ct: TestContext.Current.CancellationToken);

        result.Population.EvaluatedCandidates.Length.ShouldBe(5);
        result.Population.EvaluatedCandidates.All(solution => problem.SearchSpace.Contains(solution.Candidate)).ShouldBeTrue();
        result.Population.EvaluatedCandidates.All(solution => solution.ObjectiveVector.Count == 2).ShouldBeTrue();
    }
}
