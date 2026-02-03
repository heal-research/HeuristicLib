using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives;

public class RosenbrockFunction(int dimension) : IGradientTestFunction
{

  private const double A = 100;
  public int Dimension { get; } = dimension;
  public double Min => -2.048;
  public double Max => 2.048;
  public ObjectiveDirection Objective => ObjectiveDirection.Minimize;

  public double Evaluate(RealVector solution)
  {
    var n = solution.Count;
    var sum = 0.0;
    for (var i = 0; i < n - 1; i++) {
      var d = solution[i];
      var d1 = solution[i + 1];
      var t = d1 - d * d;
      var t2 = d - 1;
      sum += A * t * t + t2 * t2;
    }

    return sum;
  }

  public RealVector EvaluateGradient(RealVector solution)
  {
    var n = solution.Count;
    var g = new double[n];
    for (var i = 0; i < n; i++) {
      var d = solution[i];
      var sum = 0.0;
      if (i != n - 1) {
        var next = solution[i + 1];
        var cubic = 2 * A * d * d * d;
        var cross = -2 * A * next * d;
        sum += 2 * (cross + cubic + d - 1);
      }

      if (i != 0) {
        var prev = solution[i - 1];
        sum += 2 * A * (d - prev * prev);
      }

      g[i] = sum;
    }

    return g;
  }
}
