using HEAL.HeuristicLib.PythonInterOptScripts;

namespace HEAL.HeuristicLib.Extensions.Tests;

public class ExtendedSymbolicRegressionProblemTest
{
  /// <summary>
  ///  takes about 25 seconds on my machine
  /// </summary>
  [Fact]
  public void RunMagicProblem()
  {
    var file = @"TestData\192_vineyard.tsv";
    var p = ExtendedSymbolicRegressionProblem.DefaultConf(file, x => [1, 4.2, 1.3]);
    var pop = ExtendedSymbolicRegressionProblem.RunDefault(p, 43);
    Assert.Equal(300, pop.Solutions.Length);
    var best = pop.Solutions.OrderByDescending(x => x.ObjectiveVector[0]).First();

    Assert.True(best.ObjectiveVector[0] > 0.4);
    //parameters are nonsense just for testing comparison values from sklearn
    // Linear Regression Pearson r^2 (train): 0.4294
    // Random Forest Pearson r^2 (train): 0.8288
  }
}
