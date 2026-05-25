using HEAL.HeuristicLib.PythonInterop;

namespace HEAL.HeuristicLib.Tests.Scenarios.PythonInterop;

public class TestPythonWithSymbolicRegression
{
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
