namespace HEAL.HeuristicLib.Random;

public interface IRandomNumberGenerator {
  int Integer(int high, bool inclusiveHigh = false) => Integer(0, high, inclusiveHigh);
  int Integer(int low, int high, bool inclusiveHigh = false);

  double Random();
  double NextDouble() => Random();

  bool Boolean(double probability = 0.5) => Random() < probability;

  byte[] RandomBytes(int length);

  IReadOnlyList<IRandomNumberGenerator> Spawn(int count);
  IRandomNumberGenerator Spawn() => Spawn(1)[0];
}

public static class RandomNumberGeneratorExtensions {
  public static double[] Random(this IRandomNumberGenerator random, int length) {
    var values = new double[length];
    for (var i = 0; i < length; i++) {
      values[i] = random.Random();
    }

    return values;
  }

  public static int[] Integers(this IRandomNumberGenerator random, int length, int high, bool inclusiveHigh = false) {
    return Integers(random, length, 0, high, inclusiveHigh);
  }

  public static int[] Integers(this IRandomNumberGenerator random, int length, int low, int high, bool inclusiveHigh = false) {
    var values = new int[length];
    for (var i = 0; i < length; i++) {
      values[i] = random.Integer(low, high, inclusiveHigh);
    }

    return values;
  }
}
