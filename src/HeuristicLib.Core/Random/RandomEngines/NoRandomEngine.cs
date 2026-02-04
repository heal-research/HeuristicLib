namespace HEAL.HeuristicLib.Random.RandomEngines;

public class NoRandomEngine : IRandomEngine
{
  public double NextDouble() => throw new NotImplementedException();
  public int NextInt() => throw new InvalidOperationException();
}
