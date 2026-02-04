using HEAL.HeuristicLib.Genotypes.Vectors;

namespace HEAL.HeuristicLib.Random.Distributions;

public sealed class NormalDistribution(double mu = 0, double sigma = 1) : IDistribution<double>
{
  public double Sample(IRandomNumberGenerator rng) => NextDouble(rng, mu, sigma);

  public static double NextDouble(IRandomNumberGenerator random, double mu = 0, double sigma = 1)
  {
    double u;
    double s;
    do {
      u = (random.NextDouble() * 2) - 1;
      var v = (random.NextDouble() * 2) - 1;
      s = (u * u) + (v * v);
    } while (s is > 1 or 0);

    s = Math.Sqrt(-2.0 * Math.Log(s) / s);
    return mu + (sigma * u * s);
  }

  [Obsolete("This should probably be at the RealVector level rather than here.")]
  public static RealVector NextSphere(IRandomNumberGenerator uniformRandom, double[] mu, double[] sigma, int dim, bool surface = true)
  {
    var d = new RealVector(Enumerable.Range(0, dim).Select(_ => NextDouble(uniformRandom)));
    if (surface) {
      d /= d.Norm();
    }

    d *= sigma;
    d += mu;
    return d.ToArray();
  }
}

public static class NormalExtensions
{
  public static double NextGaussian(this IRandomNumberGenerator rng, double mu = 0, double sigma = 1) => NormalDistribution.NextDouble(rng, mu, sigma);
}
