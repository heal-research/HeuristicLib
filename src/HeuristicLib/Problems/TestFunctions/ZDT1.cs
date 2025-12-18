using HEAL.HeuristicLib.Encodings.RealVector;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems.TestFunctions;

public class Zdt1(int dimension) : IMultiObjectiveGradientTestFunction {
  public int Dimension { get; } = dimension;
  public double Min => 0;
  public double Max => 1;
  public Objective Objective => MultiObjective.Create([ObjectiveDirection.Minimize, ObjectiveDirection.Minimize]);

  public RealVector Evaluate(RealVector solution) {
    var count = solution.Count;
    double g = 0;
    for (var i = 1; i < count; i++)
      g += solution[i];
    g = 1.0 + 9.0 * g / (count - 1.0);

    var f0 = solution[0];
    var f1 = g * (1.0 - Math.Sqrt(solution[0] / g));
    return new RealVector(f0, f1);
  }

  public RealVector[] EvaluateGradient(RealVector solution) => Zdt1Gradient(solution);

  public static RealVector[] Zdt1Gradient(RealVector x) {
    var n = x.Count;
    var a = 9.0 / (n - 1);

    // Compute g(x)
    var sum = 0.0;
    for (var i = 1; i < n; i++)
      sum += x[i];

    var g = 1.0 + a * sum;

    // Allocate gradients
    var gradF1 = new double[n];
    var gradF2 = new double[n];

    // ---- f1 gradient ----
    gradF1[0] = 1.0;
    for (var i = 1; i < n; i++)
      gradF1[i] = 0.0;

    // ---- f2 gradient ----
    var x1 = x[0];

    // Guard against division by zero at x1 = 0
    var eps = 1e-12;
    var sqrtGX1 = Math.Sqrt(g * Math.Max(x1, eps));

    // ∂f2 / ∂x1
    gradF2[0] = -g / (2.0 * sqrtGX1);

    // ∂f2 / ∂xi  (i >= 2)
    var factor = a * (1.0 - 0.5 * Math.Sqrt(x1 / g));
    for (var i = 1; i < n; i++)
      gradF2[i] = factor;

    return [gradF1, gradF2];
  }
}
