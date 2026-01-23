namespace HEAL.HeuristicLib.Random;

public class RandomNumberGenerator : IRandomNumberGenerator
{
  private readonly ulong key;
  private readonly IKeyCombiner keyCombiner;
  private readonly Func<ulong, IRandomEngine> engineFactory;
  private readonly IRandomEngine engine;

  private RandomNumberGenerator(ulong key, IKeyCombiner keyCombiner, Func<ulong, IRandomEngine> engineFactory)
  {
    this.key = key;
    this.keyCombiner = keyCombiner;
    this.engineFactory = engineFactory;
    engine = engineFactory(key);
  }

  public static IRandomNumberGenerator Create(ulong seed) => Create(seed, RandomProfile.Default);

  public static IRandomNumberGenerator Create(ulong seed, RandomProfile profile) => new RandomNumberGenerator(seed, profile.KeyCombiner, profile.EngineFactory);

  public IRandomNumberGenerator Fork(ulong forkKey)
  {
    var combinedKey = keyCombiner.Combine(key, forkKey);
    return new RandomNumberGenerator(combinedKey, keyCombiner, engineFactory);
  }

  public double NextDouble() => engine.NextDouble();

  public int NextInt() => engine.NextInt();
}
