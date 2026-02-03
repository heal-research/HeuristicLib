namespace HEAL.HeuristicLib.Random;

public class SystemRandomNumberGenerator : IRandomNumberGenerator
{
  private static readonly System.Random GlobalSeedGenerator = new();
  private readonly System.Random random;

  private readonly SeedSequence seedSequence;

  public SystemRandomNumberGenerator(SeedSequence seedSequence)
  {
    this.seedSequence = seedSequence;
    var seed = seedSequence.GenerateSeed();
    random = new System.Random(seed);
  }

  public SystemRandomNumberGenerator(int seed) : this(new SeedSequence(seed)) {}

  public SystemRandomNumberGenerator() : this(NextGlobal()) {}

  public byte[] RandomBytes(int length)
  {
    var data = new byte[length];
    random.NextBytes(data);

    return data;
  }

  public int Integer(int low, int high, bool inclusiveHigh = false) => random.Next(low, high + (inclusiveHigh ? 1 : 0));

  public double Random() => random.NextDouble();
  IReadOnlyList<IRandomNumberGenerator> IRandomNumberGenerator.Spawn(int count) => Spawn(count);

  public static SystemRandomNumberGenerator Default(int? seed)
  {
    if (seed.HasValue) {
      return new SystemRandomNumberGenerator(seed.Value);
    }

    lock (GlobalSeedGenerator) {
      return new SystemRandomNumberGenerator(NextGlobal());
    }
  }

  private static int NextGlobal()
  {
    lock (GlobalSeedGenerator) {
      return GlobalSeedGenerator.Next();
    }
  }

  public IReadOnlyList<SystemRandomNumberGenerator> Spawn(int count) => seedSequence.Spawn(count).Select(childSequence => new SystemRandomNumberGenerator(childSequence)).ToArray();
}

public class NoRandomGenerator : IRandomNumberGenerator
{
  public static readonly NoRandomGenerator Instance = new();
  private NoRandomGenerator() {}
  public int Integer(int low, int high, bool inclusiveHigh = false) => throw new InvalidOperationException();

  public double Random() => throw new InvalidOperationException();

  public byte[] RandomBytes(int length) => throw new InvalidOperationException();

  public IReadOnlyList<IRandomNumberGenerator> Spawn(int count) => Enumerable.Repeat(this, count).ToArray();
}
