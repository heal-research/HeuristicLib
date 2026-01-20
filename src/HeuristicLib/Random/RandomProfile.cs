using HEAL.HeuristicLib.Random.KeyCombiners;
using HEAL.HeuristicLib.Random.RandomEngines;

namespace HEAL.HeuristicLib.Random;

public sealed class RandomProfile {
  public IKeyCombiner KeyCombiner { get; }
  public Func<ulong, IRandomEngine> EngineFactory { get; }
  
  public RandomProfile(IKeyCombiner keyCombiner, Func<ulong, IRandomEngine> engineFactory) {
    KeyCombiner = keyCombiner;
    EngineFactory = engineFactory;
  }
  
  public static RandomProfile Default { get; } =
    new RandomProfile(new SplitMix64KeyCombiner(), seed => new Pcg64Engine(seed));

  public static RandomProfile Philox { get; } =
    new RandomProfile(new Fmix64KeyCombiner(), seed => new PhiloxEngine(seed));
  
  [Obsolete("Slow and not cryptographically secure. Use only for testing purposes.")]
  public static RandomProfile SystemRandom { get; } =
    new RandomProfile(new SimpleKeyCombiner(), seed => new SystemRandomEngine(seed));
}
