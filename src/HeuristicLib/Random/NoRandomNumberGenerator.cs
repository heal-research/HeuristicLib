namespace HEAL.HeuristicLib.Random;

public class NoRandomNumberGenerator : IRandomNumberGenerator {
  public static readonly IRandomNumberGenerator Instance = new NoRandomNumberGenerator();
  public double Random() => throw new NotImplementedException();

  public int Integer(int low, int high, bool endpoint = false) => throw new NotImplementedException();

  public byte[] Bytes(int length) => throw new NotImplementedException();

  public IRandomNumberGenerator Fork(params int[] keys) => Instance;

  public IReadOnlyList<IRandomNumberGenerator> Spawn(int count) {
    var arr = new IRandomNumberGenerator[count];
    Array.Fill(arr, Instance);
    return arr;
  }
}
