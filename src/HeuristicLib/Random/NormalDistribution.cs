namespace HEAL.HeuristicLib.Random;

public class NormalDistribution(IRandomNumberGenerator random, double mean, double standardDeviation)
  : IDistribution {
  public double Sample() {
    double u1 = random.Random();
    double u2 = random.Random();
    double z0 = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    return mean + standardDeviation * z0;
  }
}
