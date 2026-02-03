using HEAL.HeuristicLib.Genotypes.Vectors;

namespace HEAL.HeuristicLib.Problems.TestFunctions.ZDT;

public class Zdt2(int dimension) : Zdt(dimension)
{
  protected override double G(RealVector solution)
  {
    var count = solution.Count;
    double g = 0;
    for (var i = 1; i < count; i++) {
      g += solution[i];
    }
    g = 1.0 + 9.0 * g / (count - 1.0);

    return g;
  }

  protected override RealVector GGradient(RealVector solution)
  {
    var count = solution.Count;
    var fact = 9.0 / (count - 1.0);
    var res = new double[count];
    for (var i = 1; i < res.Length; i++) {
      res[i] = fact;
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
