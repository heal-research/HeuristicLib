using HEAL.HeuristicLib.Genotypes.Vectors;

namespace HEAL.HeuristicLib.Problems.TestFunctions.ZDT;

/// <summary>
///   Note that the standard definition of ZDT4 uses values for the variables 2..n in the range [ -5, 5 ].
/// </summary>
/// <param name="dimension"></param>
public class Zdt4(int dimension) : Zdt(dimension)
{
  protected override double G(RealVector solution)
  {
    var count = solution.Count;
    double g = 0;
    for (var i = 1; i < count; i++) {
      var x = solution[i];
      g += x * x - 10 * Math.Cos(4 * Math.PI * x);
    }

    g += 10 * (count - 1) + 1;

    return g;
  }

  protected override RealVector GGradient(RealVector solution)
  {
    var count = solution.Count;
    var res = new double[count];
    for (var i = 1; i < res.Length; i++) {
      var x = solution[i];
      res[i] = 2 * x + 40 * Math.PI * Math.Sin(4 * Math.PI * x);
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
