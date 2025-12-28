namespace HEAL.HeuristicLib.Random;

public static class RandomNumberGeneratorExtensions {
  public static double[] Random(this IRandomNumberGenerator random, int length) {
    double[] values = new double[length];
    for (int i = 0; i < length; i++) {
      values[i] = random.Random();
    }

    return values;
  }

  public static int[] Integers(this IRandomNumberGenerator random, int length, int high, bool inclusiveHigh = false) {
    return Integers(random, length, 0, high, inclusiveHigh);
  }

  public static int[] Integers(this IRandomNumberGenerator random, int length, int low, int high, bool inclusiveHigh = false) {
    int[] values = new int[length];
    for (int i = 0; i < length; i++) {
      values[i] = random.Integer(low, high, inclusiveHigh);
    }

    return values;
  }
}
