using System.Reflection.Metadata.Ecma335;

namespace HEAL.HeuristicLib.Random;

public class SystemRandomNumberGenerator : IRandomNumberGenerator {
  private static readonly System.Random GlobalSeedGenerator = new();

  public static SystemRandomNumberGenerator Default(int? seed) {
    if (seed.HasValue)
      return new SystemRandomNumberGenerator(seed.Value);
    lock (GlobalSeedGenerator)
      return new SystemRandomNumberGenerator(NextGlobal());
  }

  private readonly SeedSequence seedSequence;
  private readonly System.Random random;

  public SystemRandomNumberGenerator(SeedSequence seedSequence) {
    this.seedSequence = seedSequence;
    var seed = seedSequence.GenerateSeed();
    random = new System.Random(seed);
  }

  public SystemRandomNumberGenerator(int seed) : this(new SeedSequence(seed)) { }

  private static int NextGlobal() {
    lock (GlobalSeedGenerator) {
      return GlobalSeedGenerator.Next();
    }
  }

  public SystemRandomNumberGenerator() : this(NextGlobal()) { }

  public byte[] RandomBytes(int length) {
    var data = new byte[length];
    random.NextBytes(data);
    return data;
  }

  public int Integer(int minValue, int maxValue, bool inclusiveHigh = false) {
    return random.Next(minValue, maxValue + (inclusiveHigh ? 1 : 0));
  }

  public double Random() {
    return random.NextDouble();
  }

  public IReadOnlyList<SystemRandomNumberGenerator> Spawn(int count) => seedSequence.Spawn(count).Select(childSequence => new SystemRandomNumberGenerator(childSequence)).ToArray();
  IReadOnlyList<IRandomNumberGenerator> IRandomNumberGenerator.Spawn(int count) => Spawn(count);
}

public class NoRandomGenerator : IRandomNumberGenerator {
  public static readonly NoRandomGenerator Instance = new NoRandomGenerator();
  private NoRandomGenerator() { }
  public int Integer(int low, int high, bool inclusiveHigh = false) => throw new InvalidOperationException();

  public double Random() => throw new InvalidOperationException();

  public byte[] RandomBytes(int length) => throw new InvalidOperationException();

  public IReadOnlyList<IRandomNumberGenerator> Spawn(int count) => Enumerable.Repeat(this, count).ToArray();
}
