using HEAL.HeuristicLib.Encodings.RealVector;

namespace HEAL.HeuristicLib.Problems.TestFunctions;

public class Zdt3(int dimension) : Zdt(dimension) {
  protected override double G(RealVector solution) {
    var count = solution.Count;
    double g = 0;
    for (var i = 1; i < count; i++)
      g += solution[i];
    g = 1.0 + 9.0 * g / (count - 1.0);
    return g;
  }

  protected override RealVector GGradient(RealVector solution) {
    var count = solution.Count;
    var fact = 9.0 / (count - 1.0);
    var res = new double[count];
    for (var i = 1; i < res.Length; i++)
      res[i] = fact;
    return res;
  }

  protected override double H(double f1, double g) {
    var ratio = f1 / g;
    return 1 - Math.Sqrt(ratio) - ratio * Math.Sin(10 * Math.PI * f1);
  }

  protected override (double dh_df1, double dh_dg) HGradient(double f1, double g) {
    var f1Safe = Math.Max(f1, 1e-12);
    var freq = 10 * Math.PI * f1Safe;
    var sinPart = 2 * f1Safe * Math.Sin(freq);
    var rootPart = Math.Sqrt(f1Safe * g);
    var cosPart = 20 * Math.PI * f1Safe * f1Safe * Math.Cos(freq);

    var res1 = rootPart + cosPart + sinPart;
    res1 /= -2 * g * f1Safe;
    var res2 = rootPart + sinPart;
    res2 /= 2 * g * g;
    return (res1, res2);
  }
}
