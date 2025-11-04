namespace HEAL.HeuristicLib.Random.BitGenerators;

public interface IBitGenerator {
  byte[] RandomBytes(int length);
  int RandomInteger(int minValue, int maxValue);
  double RandomDouble();
  
  IReadOnlyList<IBitGenerator> Spawn(int count);
}
