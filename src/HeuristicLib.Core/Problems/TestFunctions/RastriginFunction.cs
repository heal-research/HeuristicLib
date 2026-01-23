using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems.TestFunctions;

public class RastriginFunction(int dimension) : ITestFunction
{
  public int Dimension { get; } = dimension;
  public double Min => -5.12;
  public double Max => 5.12;
  public ObjectiveDirection Objective => ObjectiveDirection.Minimize;

  public double Evaluate(RealVector solution)
  {
    var n = solution.Count;
    const double a = 10;
    var sum = a * n;
    for (var i = 0; i < n; i++) {
      var d = solution[i];
      sum += (d * d) - (a * Math.Cos(2 * Math.PI * d));
    }

    return sum;
  }
}
