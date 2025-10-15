namespace HEAL.HeuristicLib.Random;

public class SystemRandomNumberGenerator : IRandomNumberGenerator {
  private static readonly SystemRandomNumberGenerator GlobalInstance = new();

  private readonly System.Random random;

  public SystemRandomNumberGenerator(int seed) {
    random = new System.Random(seed);
  }

  public SystemRandomNumberGenerator() {
    random = new System.Random();
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

  public int Integer() => random.Next();

  public byte[] Bytes(int length) {
    byte[] bytes = new byte[length];
    random.NextBytes(bytes);
    return bytes;
  }

  public IRandomNumberGenerator Fork(params int[] keys) {
    int newSeed = keys.Aggregate(random.Next(), (current, key) => current ^ key);
    return new SystemRandomNumberGenerator(newSeed);
  }

  public IReadOnlyList<IRandomNumberGenerator> Spawn(int count) => Enumerable
                                                                   .Range(0, count)
                                                                   .Select(_ => new SystemRandomNumberGenerator(Integer()))
                                                                   .ToArray();

  public static int RandomSeed() {
    lock (GlobalInstance) return GlobalInstance.Integer();
  }
}
