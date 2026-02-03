using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives;

public class RastriginFunction(int dimension) : IGradientTestFunction
{

  private const double A = 10;
  private const double PiTwo = 2 * Math.PI;
  public int Dimension { get; } = dimension;
  public double Min => -5.12;
  public double Max => 5.12;
  public ObjectiveDirection Objective => ObjectiveDirection.Minimize;

  public double Evaluate(RealVector solution)
  {
    var n = solution.Count;
    var sum = A * n;
    for (var i = 0; i < n; i++) {
      var d = solution[i];
      sum += d * d - A * Math.Cos(PiTwo * d);
    }

    return sum;
  }

  public RealVector EvaluateGradient(RealVector solution)
  {
    var n = solution.Count;
    var g = new double[n];
    for (var i = 0; i < n; i++) {
      var d = solution[i];
      g[i] = 2 * d + PiTwo * A * Math.Sin(PiTwo * d);
    }

    return g;
  }
}
