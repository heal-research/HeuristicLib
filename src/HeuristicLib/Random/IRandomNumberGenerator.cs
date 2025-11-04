namespace HEAL.HeuristicLib.Random;

[Obsolete]
public interface IRandomNumberGenerator {
  double Random();
  int Integer(int low, int high, bool endpoint = false);
  byte[] Bytes(int length);

  IRandomNumberGenerator Fork(params int[] keys);
  int Next(int low, int high) => Integer(low, high);
  int Next(int high) => Integer(0, high);
  IReadOnlyList<IRandomNumberGenerator> Spawn(int count);
}
