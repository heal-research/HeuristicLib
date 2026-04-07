using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.Optimization;
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
    var file = Path.Combine("TestData", "192_vineyard.tsv");

    //take the original r2 and add 4 dummy objectives that we will ignore in this test, but could be used for other things in a real scenario
    Func<SymbolicExpressionTree,ObjectiveVector, double[]> individualCallback = (t, o) => [o[0],0,0,0,0]; 
    Func<SymbolicExpressionTree[], ObjectiveVector[], double[][]> populationCallback = (ts, os) => os.Select( o => new double[]{o[0],0,0,0,0}).ToArray();

    var pop = ExtendedSymbolicRegressionProblem.RunDefault(file,40, individualCallback, populationCallback);
    Assert.Equal(300, pop.Solutions.Length);
    var best = pop.Solutions.OrderByDescending(x => x.ObjectiveVector[0]).First();

    Assert.True(best.ObjectiveVector[0] > 0.4);
    //parameters are nonsense just for testing comparison values from sklearn
    // Linear Regression Pearson r^2 (train): 0.4294
    // Random Forest Pearson r^2 (train): 0.8288
  }
}
