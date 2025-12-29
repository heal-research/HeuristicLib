namespace HEAL.HeuristicLib.Random;

public static class RandomNumberGeneratorExtensions {
  extension(IRandomNumberGenerator random) {
    public double[] Random(int length) {
      double[] values = new double[length];
      for (int i = 0; i < length; i++) {
        values[i] = random.Random();
      }

      return values;
    }

    public int[] Integers(int length, int high, bool inclusiveHigh = false) {
      return Integers(random, length, 0, high, inclusiveHigh);
    }

    public int[] Integers(int length, int low, int high, bool inclusiveHigh = false) {
      int[] values = new int[length];
      for (int i = 0; i < length; i++) {
        values[i] = random.Integer(low, high, inclusiveHigh);
      }

      return values;
    }
  }
}
