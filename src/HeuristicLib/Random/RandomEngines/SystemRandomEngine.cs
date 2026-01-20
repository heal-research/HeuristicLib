namespace HEAL.HeuristicLib.Random.RandomEngines;

public sealed class SystemRandomEngine : IRandomEngine {
  private readonly System.Random random;

  public SystemRandomEngine(ulong seed)
  {
    random = new System.Random(unchecked((int)seed));
  }

  public int NextInt() => random.Next();

  public double NextDouble() => random.NextDouble();
}
