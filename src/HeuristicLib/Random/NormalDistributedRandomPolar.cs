namespace HEAL.HeuristicLib.Random;

/// <summary>
/// Normally distributed random variable.
/// Uses Marsaglia's polar method
/// </summary>
public sealed class NormalDistributedRandomPolar {
  public static double NextDouble(IRandom uniformRandom, double mu, double sigma) {
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
}
