using HEAL.HeuristicLib.PythonInterOptScripts;

namespace HEAL.HeuristicLib.Extensions.Tests;

public class PythonInterOptEquationScoringTest
{
  /// <summary>
  ///  takes about 25 seconds on my machine
  /// </summary>
  [Fact]
  public void RunProblem()
  {
    var file = Path.Combine("TestData", "192_vineyard.tsv");
    var p = PythonInterOptEquationScoring.DefaultConf(file, 30, (x, y) => [y[0], y[0], 0.9, 0.9 , 0.9]);
    var pop = PythonInterOptEquationScoring.RunDefault(p, 42);
    Assert.Equal(300, pop.Solutions.Length);
    var best = pop.Solutions.OrderByDescending(x => x.ObjectiveVector[0]).First();

    Assert.True(best.ObjectiveVector[0] > 0.4);
    //parameters are nonsense just for testing comparison values from sklearn
    // Linear Regression Pearson r^2 (train): 0.4294
    // Random Forest Pearson r^2 (train): 0.8288
  }
}
