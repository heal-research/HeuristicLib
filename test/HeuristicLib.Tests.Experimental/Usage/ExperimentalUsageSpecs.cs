using HEAL.HeuristicLib.Encodings.Permutations;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Problems.QuadraticAssignment;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives;

namespace HEAL.HeuristicLib.Tests.Usage;

public sealed class ExperimentalUsageSpecs
{
    [Fact]
    public void QuadraticAssignment_IsAvailableFromExperimental()
    {
        var data = new QuadraticAssignmentProblemData(
            new double[,] { { 0, 1 }, { 2, 0 } },
            new double[,] { { 0, 10 }, { 20, 0 } });
        var problem = new QuadraticAssignmentProblem(data);

        var objective = problem.Evaluate(new Permutation(0, 1), RandomNumberGenerator.Create(42));

        objective.ShouldBe((ObjectiveVector)50.0);
    }

    [Fact]
    public void ResearchAlgorithm_IsConfiguredFromExperimental()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(4));
        var algorithm = new OpenEndedRelevantAllelesPreservingGeneticAlgorithm<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
        {
            PopulationSize = 20,
            Creator = new UniformDistributedCreator(problem.SearchSpace),
            Crossover = new AlphaBetaBlendCrossover(),
            Mutator = new GaussianMutator(0.2, 0.1),
            Selector = TournamentSelector.For(problem, tournamentSize: 2),
            MaxEffort = 100,
            MaximumGenerations = 2
        };

        algorithm.PopulationSize.ShouldBe(20);
        algorithm.MaxEffort.ShouldBe(100);
    }
}
