namespace HEAL.HeuristicLib.Random;

public static class RandomGeneratorExtensions {
  public static double[] Random(this IRandomNumberGenerator random, int length) {
    double[] values = new double[length];
    for (int i = 0; i < length; i++) {
      values[i] = random.Random();
    }

    return values;
  }

  public static int Integer(this IRandomNumberGenerator random, int high, bool endpoint = false) {
    return random.Integer(0, high, endpoint);
  }

  public static int[] Integers(this IRandomNumberGenerator random, int length, int low, int high, bool endpoint = false) {
    int[] values = new int[length];
    for (int i = 0; i < length; i++) {
      values[i] = random.Integer(low, high, endpoint);
    }

    return values;
  }

  public static int[] Integers(this IRandomNumberGenerator random, int length, int high, bool endpoint = false) {
    return random.Integers(length, 0, high, endpoint);
  }
}
