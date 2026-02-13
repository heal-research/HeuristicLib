using HEAL.HeuristicLib.Random.KeyCombiners;
using HEAL.HeuristicLib.Random.RandomEngines;

namespace HEAL.HeuristicLib.Random;

public sealed class RandomProfile
{
  public IKeyCombiner KeyCombiner { get; }
  public Func<ulong, IRandomEngine> EngineFactory { get; }

  public RandomProfile(IKeyCombiner keyCombiner, Func<ulong, IRandomEngine> engineFactory)
  {
    KeyCombiner = keyCombiner;
    EngineFactory = engineFactory;
  }

  public static RandomProfile Default { get; } = new(new SplitMix64KeyCombiner(), seed => new Pcg32Engine(seed));
  public static RandomProfile System { get; } = new(new SplitMix64KeyCombiner(), seed => new SystemRandomEngine(seed));

  public static RandomProfile Philox { get; } = new(new Fmix64KeyCombiner(), seed => new PhiloxEngine(seed));
}
