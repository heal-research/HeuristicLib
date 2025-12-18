using HEAL.HeuristicLib.Encodings.RealVector;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems.TestFunctions;

public class Zdt2(int dimension) : IMultiObjectiveGradientTestFunction {
  public int Dimension { get; } = dimension;
  public double Min => 0;
  public double Max => 1;
  public Objective Objective => MultiObjective.Create([ObjectiveDirection.Minimize, ObjectiveDirection.Minimize]);

  public RealVector Evaluate(RealVector solution) {
    double g = 0;
    for (var i = 1; i < solution.Count; i++)
      g += solution[i];
    g = 1.0 + 9.0 * g / (solution.Count - 1);
    var d = solution[0] / g;
    return new RealVector(solution[0], g * (1.0 - d * d));
  }

  public RealVector[] EvaluateGradient(RealVector solution) {
    var (z1, z2) = Zdt2Gradient(solution);
    return [z1, z2];
  }

  public static (RealVector gradF0, RealVector gradF1) Zdt2Gradient(RealVector x) {
    var n = x.Count;
    var a = 9.0 / (n - 1);

    // Compute g(x)
    var sum = 0.0;
    for (var i = 1; i < n; i++)
      sum += x[i];

    var g = 1.0 + a * sum;

    var gradF0 = new double[n];
    var gradF1 = new double[n];

    // ---- f0 gradient ----
    gradF0[0] = 1.0;
    for (var i = 1; i < n; i++)
      gradF0[i] = 0.0;

    // ---- f1 gradient ----
    var x0 = x[0];

    // ∂f1 / ∂x0
    gradF1[0] = -2.0 * x0 / g;

    // ∂f1 / ∂xi  (i >= 1)
    var factor = a * (1.0 + x0 * x0 / (g * g));
    for (var i = 1; i < n; i++)
      gradF1[i] = factor;

    return (gradF0, gradF1);
  }
}
