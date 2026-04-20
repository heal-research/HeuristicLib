namespace HEAL.HeuristicLib.Random;

public static class RandomExtensions
{
  extension(IRandomNumberGenerator random)
  {
    public int NextInt(int high, bool inclusiveHigh = false) => random.NextInt(0, high, inclusiveHigh);

    public int NextInt(int low, int high, bool inclusiveHigh = false)
    {
      var range = ValidateAndGetIntRange(low, high, inclusiveHigh);
      return NextIntUnchecked(random, low, range);
    }

    public bool NextBool(double probability = 0.5)
    {
      ValidateProbability(probability);
      return NextBoolUnchecked(random, probability);
    }

    public bool[] NextBools(int length, double probability = 0.5)
    {
      ValidateProbability(probability);

      var values = new bool[length];
      random.FillBoolsUnchecked(values, probability);

      return values;
    }

    public double NextDouble(double low, double high)
    {
      var width = ValidateAndGetWidth(low, high);
      return NextDoubleUnchecked(random, low, width);
    }

    public double NextNormal(double mu = 0, double sigma = 1)
    {
      ValidateSigma(sigma);
      return NextNormalUnchecked(random, mu, sigma);
    }

    public double[] NextNormals(int length, double mu = 0, double sigma = 1)
    {
      ValidateSigma(sigma);

      var values = new double[length];
      random.FillNormalsUnchecked(values, mu, sigma);

      return values;
    }

    public double[] NextDoubles(int length)
    {
      var values = new double[length];
      random.FillDoublesUnchecked(values);

      return values;
    }

    public double[] NextDoubles(int length, double low, double high)
    {
      var width = ValidateAndGetWidth(low, high);

      var values = new double[length];
      random.FillDoublesUnchecked(values, low, width);

      return values;
    }

    public int[] NextInts(int length, int high, bool inclusiveHigh = false) => random.NextInts(length, 0, high, inclusiveHigh);

    public int[] NextInts(int length, int low, int high, bool inclusiveHigh = false)
    {
      var range = ValidateAndGetIntRange(low, high, inclusiveHigh);

      var values = new int[length];
      random.FillIntsUnchecked(values, low, range);

      return values;
    }

    internal int NextIntUnchecked(int low, long range)
      => low + (int)(random.NextDouble() * range);

    internal bool NextBoolUnchecked(double probability)
      => random.NextDouble() < probability;

    internal double NextDoubleUnchecked(double low, double width)
      => random.NextDouble() * width + low;

    internal double NextNormalUnchecked(double mu, double sigma)
    {
      double u;
      double s;
      do {
        u = (random.NextDouble() * 2) - 1;
        var v = (random.NextDouble() * 2) - 1;
        s = (u * u) + (v * v);
      } while (s is > 1 or 0);

      s = Math.Sqrt(-2.0 * Math.Log(s) / s);
      return mu + (sigma * u * s);
    }

    private static long ValidateAndGetIntRange(int low, int high, bool inclusiveHigh = false)
    {
      var range = (long)high - low + (inclusiveHigh ? 1L : 0L);

      if (range <= 0)
        throw new ArgumentOutOfRangeException(nameof(high));

      return range;
    }

    private static double ValidateAndGetWidth(double low, double high)
    {
      if (high < low)
        throw new ArgumentOutOfRangeException(nameof(high));

      return high - low;
    }

    private static void ValidateProbability(double probability)
    {
      if (probability is < 0 or > 1)
        throw new ArgumentOutOfRangeException(nameof(probability));
    }

    private static void ValidateSigma(double sigma)
    {
      if (sigma < 0)
        throw new ArgumentOutOfRangeException(nameof(sigma));
    }

    private void FillBoolsUnchecked(Span<bool> values, double probability)
    {
      for (var i = 0; i < values.Length; i++) {
        values[i] = random.NextBoolUnchecked(probability);
      }
    }

    private void FillNormalsUnchecked(Span<double> values, double mu, double sigma)
    {
      for (var i = 0; i < values.Length; i++) {
        values[i] = random.NextNormalUnchecked(mu, sigma);
      }
    }

    private void FillDoublesUnchecked(Span<double> values)
    {
      for (var i = 0; i < values.Length; i++) {
        values[i] = random.NextDouble();
      }
    }

    private void FillDoublesUnchecked(Span<double> values, double low, double width)
    {
      for (var i = 0; i < values.Length; i++) {
        values[i] = random.NextDoubleUnchecked(low, width);
      }
    }

    private void FillIntsUnchecked(Span<int> values, int low, long range)
    {
      for (var i = 0; i < values.Length; i++) {
        values[i] = random.NextIntUnchecked(low, range);
      }
    }
  }
}
