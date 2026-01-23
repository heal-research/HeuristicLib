using HEAL.HeuristicLib.Encodings.RealVector;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives;

public class AckleyFunction(int dimension) : IGradientTestFunction {
  public int Dimension { get; } = dimension;
  public double Min => -32.768;
  public double Max => 32.768;
  public ObjectiveDirection Objective => ObjectiveDirection.Minimize;

  private const double A = 20;
  private const double B = 0.2;
  private const double PiTwo = 2 * Math.PI;

  public double Evaluate(RealVector solution) {
    var n = solution.Count;
    var sumSquares = 0.0;
    var sumCosines = 0.0;
    for (var i = 0; i < n; i++) {
      var d = solution[i];
      sumSquares += d * d;
      sumCosines += Math.Cos(PiTwo * d);
    }

    sumSquares /= n;
    sumSquares = Math.Sqrt(sumSquares);
    sumCosines /= n;

    return -A * Math.Exp(-B * sumSquares) - Math.Exp(sumCosines) + A + Math.E;
  }

  public RealVector EvaluateGradient(RealVector solution) {
    var n = solution.Count;
    var sumSquares = 0.0;
    var sumCosines = 0.0;
    for (var i = 0; i < n; i++) {
      var d = solution[i];
      sumSquares += d * d;
      sumCosines += Math.Cos(PiTwo * d);
    }

    sumSquares /= n;
    sumSquares = Math.Sqrt(sumSquares);
    if (sumSquares.IsAlmost(0.0, 1e-15)) return new double[n];

    sumCosines /= n;

    var res = new double[n];
    for (int i = 0; i < n; i++) {
      var x = solution[i];
      var s1 = A * B * x * Math.Exp(-B * sumSquares) / (n * sumSquares);
      var s2 = PiTwo * Math.Sin(PiTwo * x) * Math.Exp(sumCosines) / n;
      res[i] = s1 + s2;
    }

    return res;
  }
}
