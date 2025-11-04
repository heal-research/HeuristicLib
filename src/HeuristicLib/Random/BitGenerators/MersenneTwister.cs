using HEAL.HeuristicLib.Random.BitGenerators;

namespace HEAL.HeuristicLib.Random;

public class MersenneTwister : IBitGenerator {
  public byte[] RandomBytes(int length) => throw new NotImplementedException();
  public int RandomInteger(int minValue, int maxValue) => throw new NotImplementedException();
  public double RandomDouble() => throw new NotImplementedException();

  public IReadOnlyList<IBitGenerator> Spawn(int count) => throw new NotImplementedException();
}
