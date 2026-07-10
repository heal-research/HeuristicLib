using HEAL.HeuristicLib.Random.KeyCombiners;
using HEAL.HeuristicLib.Random.RandomEngines;

namespace HEAL.HeuristicLib.Random;

public static class RandomHelpers
{
    private static RandomProfile NoRandomProfile { get; } = new(new SimpleKeyCombiner(), _ => new NoRandomEngine());

    public static IRandomNumberGenerator NoRandom { get; } = RandomNumberGenerator.Create(0, NoRandomProfile);
}
