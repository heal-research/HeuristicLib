namespace HEAL.HeuristicLib.Random.Distributions;

public class UniformDistribution(IRandomNumberGenerator random, double low, double high) : IDistribution
{
  public double Sample() => low + (high - low) * random.Random();
}
