using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives;

public class GriewankFunction(int dimension) : IGradientTestFunction
{

  private const double A = 4000;
  public int Dimension { get; } = dimension;
  public double Min => -600;
  public double Max => 600;
  public ObjectiveDirection Objective => ObjectiveDirection.Minimize;

  public double Evaluate(RealVector solution)
  {
    var n = solution.Count;
    var sum = 0.0;
    var product = 1.0;
    for (var i = 0; i < n; i++) {
      var d = solution[i];
      sum += d * d;
      product *= Math.Cos(d / Math.Sqrt(i + 1));
    }

    sum /= A;

    return sum - product + 1;
  }

  public RealVector EvaluateGradient(RealVector solution)
  {
    var n = solution.Count;

    // cos_i and prefix/suffix products
    var cosv = new double[n];
    var prefix = new double[n + 1];
    var suffix = new double[n + 1];

    prefix[0] = 1.0;
    for (var i = 0; i < n; i++) {
      var root = Math.Sqrt(i + 1.0);
      cosv[i] = Math.Cos(solution[i] / root);
      prefix[i + 1] = prefix[i] * cosv[i];
    }

    suffix[n] = 1.0;
    for (var i = n - 1; i >= 0; i--) {
      suffix[i] = suffix[i + 1] * cosv[i];
    }

    var res = new double[n];
    for (var i = 0; i < n; i++) {
      var d = solution[i];
      var root = Math.Sqrt(i + 1.0);
      var sin = Math.Sin(d / root);

      // product over j != i of cos(x_j/sqrt(j+1))
      var prodExcl = prefix[i] * suffix[i + 1];

      var termProd = prodExcl * (sin / root);// this is -dP/dx_i with the outer minus already applied
      var termSum = 2.0 * d / A;

      res[i] = termSum + termProd;
    }

    return res;
  }
}
