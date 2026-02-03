using HEAL.HeuristicLib.Genotypes.Vectors;

namespace HEAL.HeuristicLib.Problems.TestFunctions.ZDT;

public class Zdt6(int dimension) : Zdt(dimension)
{
  protected override double F1(RealVector solution)
  {
    var x = solution[0];

    return 1 - Math.Exp(-4 * x) * Math.Pow(Math.Sin(6 * Math.PI * x), 6);
  }

  protected override RealVector F1Gradient(RealVector solution)
  {
    var x = solution[0];
    var freq = 6 * Math.PI * x;
    var sin = Math.Sin(freq);
    var res = new double[solution.Count];
    var cos = Math.Cos(freq);
    res[0] = 4 * Math.Exp(-4 * x) * Math.Pow(sin, 5) * (sin - 9 * Math.PI * cos);

    return res;
  }

  protected override double G(RealVector solution)
  {
    var count = solution.Count;
    double g = 0;
    for (var i = 1; i < count; i++) {
      g += solution[i];
    }

    g = 1 + 9.0 * Math.Pow(g / (count - 1.0), 0.25);

    return g;
  }

  protected override RealVector GGradient(RealVector solution)
  {
    var n = solution.Count;
    var sum = 0.0;
    for (var i = 1; i < n; i++) {
      sum += solution[i];
    }

    var mean = sum / (n - 1.0);

    // mean can be 0 -> derivative blows up; either allow Infinity or clamp.
    var meanSafe = Math.Max(mean, 1e-12);
    var resEntry = 2.25 / ((n - 1.0) * Math.Pow(meanSafe, 0.75));
    var res = new double[n];
    for (var i = 1; i < n; i++) {
      res[i] = resEntry;
    }

    return res;
  }

  protected override double H(double f1, double g)
  {
    var ratio = f1 / g;

    return 1 - ratio * ratio;
  }

  protected override (double dh_df1, double dh_dg) HGradient(double f1, double g) => (-2 * f1 / (g * g), 2 * f1 * f1 / (g * g * g));
}
