using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.PythonInterop;

namespace HEAL.HeuristicLib.Tests.Scenarios.PythonInterop;

public class TestPythonWithSymbolicRegression
{
    [Fact]
    public void MultiObjectiveReports_OrderTheInitialPopulationAndGenealogy()
    {
        var problem = ProblemGeneration.SphereRastriginProblem(3, -5, 5, 0.5);
        var result = PythonGenealogyAnalysis.RunAlgorithmConfigurable(problem, null,
            new TestFunctionExperimentParameters
            {
                AlgorithmName = "nsga2",
                Creator = new UniformDistributedCreator(),
                Crossover = new AlphaBetaBlendCrossover { Alpha = 0.7 },
                Mutator = new GaussianMutator(0.2, 0.15),
                PopulationSize = 10,
                Iterations = 1,
                TrackGenealogy = true,
                ObjectiveComparer = new LexicographicComparer(problem.Objective.Directions),
                Seed = AlgorithmRandomSeed
            });

        result.BestMedianWorst.Count.ShouldBe(1);
        result.Graph.ShouldContain("digraph");
    }

    private const int AlgorithmRandomSeed = 42;

    [Fact]
    public void TestPlayground()
    {
        const int iterations = 200;
        var i = 0;
        var file = Path.Combine("TestData", "192_vineyard.tsv");
        _ = PythonGenealogyAnalysis.RunSymbolicRegressionConfigurable(file,
          new SymRegExperimentParameters
          {
              Seed = AlgorithmRandomSeed,
              Iterations = iterations
          },
          callback: _ => i++);
        i.ShouldBe(iterations);
    }

    [Fact]
    public void TestPlayground2()
    {
        const int iterations = 4;
        var i = 0;
        PythonCorrelationAnalysis.RunCorrelationNsga2((_, _) => { i++; }, iterations, 100, ProblemGeneration.SphereRastriginProblem(10, -5, 5, 0.5));
        i.ShouldBe(iterations);
    }
}
