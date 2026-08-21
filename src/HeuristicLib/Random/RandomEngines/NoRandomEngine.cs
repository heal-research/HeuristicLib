namespace HEAL.HeuristicLib.Random;

public class NoRandomEngine : IRandomEngine
{
    public double NextDouble() => throw new InvalidOperationException("No random engine cannot generate values.");
    public int NextInt() => throw new InvalidOperationException("No random engine cannot generate values.");
}
