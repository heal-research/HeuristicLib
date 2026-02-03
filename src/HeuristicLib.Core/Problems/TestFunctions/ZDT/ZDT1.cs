using HEAL.HeuristicLib.Genotypes.Vectors;

namespace HEAL.HeuristicLib.Problems.TestFunctions.ZDT;

public class Zdt1(int dimension) : Zdt(dimension)
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

  protected override double H(double f1, double g) => 1 - Math.Sqrt(f1 / g);

  protected override (double dh_df1, double dh_dg) HGradient(double f1, double g)
  {
    var f1Safe = Math.Max(f1, 1e-12);
    var r = Math.Sqrt(f1Safe / g);

    return (-0.5 * r / f1Safe, 0.5 * r / g);
  }
}
