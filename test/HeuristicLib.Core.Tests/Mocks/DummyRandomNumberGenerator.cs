using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Tests.Mocks;

public sealed class DummyRandomNumberGenerator : IRandomNumberGenerator
{
  public static readonly DummyRandomNumberGenerator Instance = new();
  private DummyRandomNumberGenerator() { }
  public double NextDouble() => throw new NotImplementedException();

  public int NextInt() => throw new NotImplementedException();

  public IRandomNumberGenerator Fork(ulong forkKey) => throw new NotImplementedException();
}
