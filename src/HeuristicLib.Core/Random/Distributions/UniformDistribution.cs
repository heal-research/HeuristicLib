namespace HEAL.HeuristicLib.Random.Distributions;

public class UniformRealDistribution(double low, double high) : IDistribution<double>
{
  public UniformRealDistribution(double high)
    : this(0.0, high)
  {
  }

  public UniformRealDistribution()
    : this(0.0, 1.0)
  {
  }

  public double Sample(IRandomNumberGenerator rng) => low + ((high - low) * rng.NextDouble());
}

public class UniformIntegerDistribution(int low, int high, bool inclusiveHigh = false) : IDistribution<int>
{
  public UniformIntegerDistribution(int high, bool inclusiveHigh = false)
    : this(0, high, inclusiveHigh)
  {
  }

  public UniformIntegerDistribution()
    : this(0, 1)
  {
  }

  public int Sample(IRandomNumberGenerator rng) => rng.NextInt(low, high, inclusiveHigh);
}
