using HEAL.HeuristicLib.PythonInterOptScripts;

namespace HEAL.HeuristicLib.Extensions.Tests;

public class TestPythonWithSymbolicRegression
{
  private const int AlgorithmRandomSeed = 42;
  
  [Fact]
  public void TestPlayground()
  {
    const int iterations = 200;
    var i = 0;
    var file = @"TestData\192_vineyard.tsv";
    var res = PythonGenealogyAnalysis.RunSymbolicRegressionConfigurable(file,
    new PythonGenealogyAnalysis.SymRegExperimentParameters {
      Seed = AlgorithmRandomSeed,
      Iterations = iterations
    },
    callback: _ => i++);
    Assert.Equal(iterations, i);
  }

  [Fact]
  public void TestPlayground2()
  {
    const int iterations = 4;
    var i = 0;
    PythonCorrelationAnalysis.RunCorrelationNsga2((_, _) => { i++; }, iterations, 100);
    Assert.Equal(iterations, i);
  }

}
