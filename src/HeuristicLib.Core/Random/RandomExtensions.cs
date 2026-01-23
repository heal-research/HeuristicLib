namespace HEAL.HeuristicLib.Random;

public static class RandomExtensions
{
  extension(IRandomNumberGenerator random)
  {
    public int NextInt(int high, bool inclusiveHigh = false) => random.NextInt(0, high, inclusiveHigh);

    public int NextInt(int low, int high, bool inclusiveHigh = false) => (random.NextInt() % (high - low + (inclusiveHigh ? 1 : 0))) + low;

    public bool NextBool(double probability = 0.5) => random.NextDouble() < probability;


    public double[] NextDoubles(int length)
    {
      var values = new double[length];
      for (var i = 0; i < length; i++) {
        values[i] = random.NextDouble();
      }

      return values;
    }

    public int[] NextInts(int length, int high, bool inclusiveHigh = false) => random.NextInts(length, 0, high, inclusiveHigh);

    public int[] NextInts(int length, int low, int high, bool inclusiveHigh = false)
    {
      var values = new int[length];
      for (var i = 0; i < length; i++) {
        values[i] = random.NextInt(low, high, inclusiveHigh);
      }

      return values;
    }
  }
}
