using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Tests.TestSupport.Mocks;

public sealed class DummyRandomNumberGenerator : IRandomNumberGenerator
{
    public static readonly DummyRandomNumberGenerator Instance = new();
    private DummyRandomNumberGenerator() { }
    public double NextDouble() => throw new NotSupportedException();

    public int NextInt() => throw new NotSupportedException();

    public IRandomNumberGenerator Fork(ulong forkKey) => throw new NotSupportedException();
}
