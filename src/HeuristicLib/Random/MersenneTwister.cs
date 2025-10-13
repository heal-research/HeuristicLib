namespace HEAL.HeuristicLib.Random;

public class MersenneTwister(int seed) : IRandomNumberGenerator {
  public double Random() {
    throw new NotImplementedException();
  }

  public int Integer(int low, int high, bool endpoint = false) {
    throw new NotImplementedException();
  }

  public byte[] Bytes(int length) {
    throw new NotImplementedException();
  }

  public IRandomNumberGenerator Fork(params int[] keys) {
    throw new NotImplementedException();
  }

  public IReadOnlyList<IRandomNumberGenerator> Spawn(int count) => Enumerable.Range(0, count).Select(x => new MersenneTwister(Integer(0, int.MaxValue))).ToArray();
}
