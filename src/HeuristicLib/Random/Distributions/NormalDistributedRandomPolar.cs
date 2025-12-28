using HEAL.HeuristicLib.Genotypes.Vectors;

namespace HEAL.HeuristicLib.Random.Distributions;

/// <summary>
/// Normally distributed random variable.
/// Uses Marsaglia's polar method
/// </summary>
public sealed class NormalDistributedRandomPolar {
  public static double NextDouble(IRandomNumberGenerator uniformRandom, double mu = 0, double sigma = 1) {
    // we don't use spare numbers (efficiency loss but easier for multithreaded code)
    double u;
    double s;
    do {
      u = uniformRandom.Random() * 2 - 1;
      var v = uniformRandom.Random() * 2 - 1;
      s = u * u + v * v;
    } while (s is > 1 or 0);

    s = Math.Sqrt(-2.0 * Math.Log(s) / s);
    return mu + sigma * u * s;
  }

  public static RealVector NextSphere(IRandomNumberGenerator uniformRandom, double[] mu, double[] sigma, int dim, bool surface = true) {
    var d = new RealVector(Enumerable.Range(0, dim).Select(_ => NextDouble(uniformRandom)));
    if (surface)
      d /= d.Norm();
    d *= sigma;
    d += mu;
    return d.ToArray();
  }
}
