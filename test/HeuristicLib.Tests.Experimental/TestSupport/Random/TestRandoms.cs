namespace HEAL.HeuristicLib.Tests.TestSupport.Random;

public static class TestRandoms
{
    public static RandomProfile SystemRandom { get; } = new(new SimpleKeyCombiner(), seed => new SystemRandomEngine(seed));

    public static RandomProfile NoRandomProfile { get; } = new(new SimpleKeyCombiner(), _ => new NoRandomEngine());

    public static IRandomNumberGenerator NoRandom { get; } = RandomNumberGenerator.Create(0, NoRandomProfile);

    public static IRandomNumberGenerator SystemRandomGenerator(ulong seed) => RandomNumberGenerator.Create(seed, SystemRandom);
}
