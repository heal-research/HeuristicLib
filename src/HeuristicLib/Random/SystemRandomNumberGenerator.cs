namespace HEAL.HeuristicLib.Random;

public class SystemRandomNumberGenerator(int seed) : IRandomNumberGenerator {
  private static readonly SystemRandomNumberGenerator GlobalInstance = new();

  private readonly System.Random random = new(seed);

  public SystemRandomNumberGenerator() : this(RandomSeed()) { }

  public double Random() => random.NextDouble();

  public int Integer(int low, int high, bool endpoint = false) => random.Next(low, endpoint ? high : high + 1);

  public int Integer() => random.Next();

  public byte[] Bytes(int length) {
    byte[] bytes = new byte[length];
    random.NextBytes(bytes);
    return bytes;
  }

  public IRandomNumberGenerator Fork(params int[] keys) => new SystemRandomNumberGenerator(keys.Aggregate(random.Next(), (current, key) => current ^ key));

  public IReadOnlyList<IRandomNumberGenerator> Spawn(int count) => Enumerable
                                                                   .Range(0, count)
                                                                   .Select(_ => new SystemRandomNumberGenerator(Integer()))
                                                                   .ToArray();

  public static int RandomSeed() {
    lock (GlobalInstance) return GlobalInstance.Integer();
  }
}
