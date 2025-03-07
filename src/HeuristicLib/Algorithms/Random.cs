namespace HEAL.HeuristicLib.Algorithms;

public interface IRandomSource {
  IRandomNumberGenerator CreateRandomNumberGenerator();
}

public class RandomSource : IRandomSource {
  private readonly Random random;
  public int Seed { get; }
  
  public RandomSource(int seed) {
    Seed = seed;
    random = new Random(seed);
  }
  public RandomSource() : this(GetRandomSeed()) { }
  
  public IRandomNumberGenerator CreateRandomNumberGenerator() {
    return new SystemRandomNumberGenerator(random);
  }

  private static int GetRandomSeed() => new Random().Next();
}

public class FixedRandomSource : IRandomSource {
  public int Seed { get; }
  public FixedRandomSource(int seed) {
    Seed = seed;
  }

  public IRandomNumberGenerator CreateRandomNumberGenerator() {
    return new SystemRandomNumberGenerator(new Random(Seed));
  }
}

public interface IRandomNumberGenerator {
  double Random();
  int Integer(int low, int high, bool endpoint = false);
  byte[] Bytes(int length);
  
  IRandomNumberGenerator[] Spawn(int count);
}

public static class RandomGeneratorExtensions {
  public static double[] Random(this IRandomNumberGenerator random, int length) {
    double[] values = new double[length];
    for (int i = 0; i < length; i++) {
      values[i] = random.Random();
    }
    return values;
  }

  public static int Integer(this IRandomNumberGenerator random, int high, bool endpoint = false) {
    return random.Integer(0, high, endpoint);
  }
  
  public static int[] Integers(this IRandomNumberGenerator random, int length, int low, int high, bool endpoint = false) {
    int[] values = new int[length];
    for (int i = 0; i < length; i++) {
      values[i] = random.Integer(low, high, endpoint);
    }
    return values;
  }
  
  public static int[] Integers(this IRandomNumberGenerator random, int length, int high, bool endpoint = false) {
    return random.Integers(length, 0, high, endpoint);
  }
}

public class SystemRandomNumberGenerator : IRandomNumberGenerator {
  private readonly Random random;
  public SystemRandomNumberGenerator(Random random) {
    this.random = random;
  }
  
  public double Random() {
    return random.NextDouble();
  }
  
  public int Integer(int low, int high, bool endpoint = false) {
    return endpoint switch {
      false => random.Next(low, high),
      true => random.Next(low, high + 1),
    };
  }
  
  public byte[] Bytes(int length) {
    byte[] bytes = new byte[length];
    random.NextBytes(bytes);
    return bytes;
  }
  
  public IRandomNumberGenerator[] Spawn(int count) {
    return this
      .Integers(count, int.MaxValue)
      .Select(seed => new SystemRandomNumberGenerator(new Random(seed)))
      .ToArray<IRandomNumberGenerator>();
  }
}

public class MersenneTwister : IRandomNumberGenerator {
  public double Random() {
    throw new NotImplementedException();
  }
  public int Integer(int low, int high, bool endpoint = false) {
    throw new NotImplementedException();
  }
  public byte[] Bytes(int length) {
    throw new NotImplementedException();
  }
  public IRandomNumberGenerator[] Spawn(int count) {
    throw new NotImplementedException();
  }
}

public class PermutedCongruentialGenerator : IRandomNumberGenerator {
  public double Random() { throw new NotImplementedException(); }
  public int Integer(int low, int high, bool endpoint = false) { throw new NotImplementedException(); }
  public byte[] Bytes(int length) { throw new NotImplementedException(); }
  public IRandomNumberGenerator[] Spawn(int count) { throw new NotImplementedException(); }
}

public class PhiloxGenerator : IRandomNumberGenerator {
  public double Random() { throw new NotImplementedException(); }
  public int Integer(int low, int high, bool endpoint = false) { throw new NotImplementedException(); }
  public byte[] Bytes(int length) { throw new NotImplementedException(); }
  public IRandomNumberGenerator[] Spawn(int count) { throw new NotImplementedException(); }
}

public interface IDistribution {
  
}

public class UniformDistribution : IDistribution {
  private readonly IRandomNumberGenerator random;
  private readonly double low;
  private readonly double high;
  
  public UniformDistribution(IRandomNumberGenerator random, double low, double high) {
    this.random = random;
    this.low = low;
    this.high = high;
  }
  
  public double Sample() {
    return low + (high - low) * random.Random();
  }
}

public class NormalDistribution : IDistribution {
  private readonly IRandomNumberGenerator random;
  private readonly double mean;
  private readonly double standardDeviation;
  
  public NormalDistribution(IRandomNumberGenerator random, double mean, double standardDeviation) {
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
