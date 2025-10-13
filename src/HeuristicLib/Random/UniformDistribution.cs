namespace HEAL.HeuristicLib.Random;

public class UniformDistribution(IRandomNumberGenerator random, double low, double high) : IDistribution {
  public double Sample() {
    return low + (high - low) * random.Random();
  }
}
