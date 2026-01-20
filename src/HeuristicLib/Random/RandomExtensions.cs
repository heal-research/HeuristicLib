namespace HEAL.HeuristicLib.Random;

public static class RandomExtensions {
  extension(IRandomNumberGenerator random) {
    public int NextInt(int high, bool inclusiveHigh = false) {
      return random.NextInt(0, high, inclusiveHigh);
    }
    
    public int NextInt(int low, int high, bool inclusiveHigh = false) {
      return random.NextInt() % (high - low + (inclusiveHigh ? 1 : 0)) + low;
    }
    
    public bool NextBool(double probability = 0.5) {
      return random.NextDouble() < probability;
    }

    
    public double[] NextDoubles(int length) {
      double[] values = new double[length];
      for (int i = 0; i < length; i++) {
        values[i] = random.NextDouble();
      }
    
      return values;
    }
    
    public int[] NextInts(int length, int high, bool inclusiveHigh = false) {
      return NextInts(random, length, 0, high, inclusiveHigh);
    }
    
    public int[] NextInts(int length, int low, int high, bool inclusiveHigh = false) {
      int[] values = new int[length];
      for (int i = 0; i < length; i++) {
        values[i] = random.NextInt(low, high, inclusiveHigh);
      }
    
      return values;
    }
  }
}
