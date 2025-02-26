namespace HEAL.HeuristicLib.Algorithms;

public interface IRandomGenerator {
  double Random();
  int Integer(int low, int? high = null, bool endpoint = false);
  byte[] Bytes(int length);
  
  IReadOnlyList<IRandomGenerator> Spawn(int count);
}

public static class RandomGeneratorExtensions
{
  public static double[] Random(this IRandomGenerator random, int length) {
    double[] values = new double[length];
    for (int i = 0; i < length; i++) {
      values[i] = random.Random();
    }
    return values;
  }

  public static int[] Integers(this IRandomGenerator random, int length, int low, int? high = null, bool endpoint = false) {
    int[] values = new int[length];
    for (int i = 0; i < length; i++) {
      values[i] = random.Integer(low, high, endpoint);
    }
    return values;
  }
}


public static class RandomGenerator {
  public static IRandomGenerator CreateDefault(int? seed = null) {
    return new SystemRandomGenerator(seed);
  }
}

public class SystemRandomGenerator : IRandomGenerator {
  private readonly Random random;
  public SystemRandomGenerator(int? seed = null) {
    random = seed is null ? new Random() : new Random(seed.Value);
  }
  
  public double Random() {
    return random.NextDouble();
  }
  
  public int Integer(int low, int? high = null, bool endpoint = false) {
    if (high is null) {
      (low, high) = (0, low);
    }
    return endpoint switch {
      false => random.Next(low, high.Value),
      true => random.Next(low, high.Value + 1),
    };
  }
  
  public byte[] Bytes(int length) {
    byte[] bytes = new byte[length];
    random.NextBytes(bytes);
    return bytes;
  }
  
  public IReadOnlyList<IRandomGenerator> Spawn(int count) {
    int[] seeds = this.Integers(count, int.MaxValue);
    return seeds.Select(seed => new SystemRandomGenerator(seed)).ToList();
  }
}

// public class MersenneTwister : IRandomGenerator { }
//
// public class PermutedCongruentialGenerator : IRandomGenerator { }
//
// public class PhiloxGenerator : IRandomGenerator { }

public interface IDistribution {
  
}

public class UniformDistribution : IDistribution {
  private readonly IRandomGenerator random;
  private readonly double low;
  private readonly double high;
  
  public UniformDistribution(IRandomGenerator random, double low, double high) {
    this.random = random;
    this.low = low;
    this.high = high;
  }
  
  public double Sample() {
    return low + (high - low) * random.Random();
  }
}

public class NormalDistribution : IDistribution {
  private readonly IRandomGenerator random;
  private readonly double mean;
  private readonly double standardDeviation;
  
  public NormalDistribution(IRandomGenerator random, double mean, double standardDeviation) {
    this.random = random;
    this.mean = mean;
    this.standardDeviation = standardDeviation;
  }
  
  public double Sample() {
    double u1 = random.Random();
    double u2 = random.Random();
    double z0 = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    return mean + standardDeviation * z0;
  }
}
