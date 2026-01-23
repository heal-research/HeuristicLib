namespace HEAL.HeuristicLib.Problems.DataAnalysis.OnlineCalculators.DependencyCalculator;

public class SpearmansRankCorrelationCoefficientCalculator : IDependencyCalculator
{
  public double Maximum => 1.0;

  public double Minimum => -1.0;

  public string Name => "Spearmans Rank";

  public double Calculate(IEnumerable<double> originalValues, IEnumerable<double> estimatedValues, out OnlineCalculatorError errorState) => CalculateSpearmansRank(originalValues, estimatedValues, out errorState);

  public double Calculate(IEnumerable<Tuple<double, double>> values, out OnlineCalculatorError errorState) => CalculateSpearmansRank(values.Select(v => v.Item1), values.Select(v => v.Item2), out errorState);

  public static double CalculateSpearmansRank(IEnumerable<double> originalValues, IEnumerable<double> estimatedValues, out OnlineCalculatorError errorState)
  {
    errorState = OnlineCalculatorError.None;
    var xs = originalValues as double[] ?? originalValues.ToArray();
    var ys = estimatedValues as double[] ?? estimatedValues.ToArray();
    if (xs.Length != ys.Length) {
      throw new ArgumentException("values must be the same length");
    }

    if (xs.Length == 0) {
      throw new ArgumentException("Values must not be empty");
    }

    var rx = GetRanks(xs);
    var ry = GetRanks(ys);

    return Pearson(rx, ry);
  }

  private static double[] GetRanks(double[] values)
  {
    var n = values.Length;
    var sorted = values
      .Select((v, i) => (Value: v, Index: i))
      .OrderBy(x => x.Value)
      .ToArray();

    var ranks = new double[n];
    var i = 0;
    while (i < n) {
      var j = i;
      while (j < n && sorted[j].Value.Equals(sorted[i].Value)) {
        j++;
      }

      // average rank for ties
      var rank = ((i + j - 1) / 2.0) + 1.0;

      for (var k = i; k < j; k++) {
        ranks[sorted[k].Index] = rank;
      }

      i = j;
    }

    return ranks;
  }

  private static double Pearson(double[] xs, double[] ys)
  {
    var n = xs.Length;
    var meanX = xs.Average();
    var meanY = ys.Average();

    double num = 0.0, denX = 0.0, denY = 0.0;
    for (var i = 0; i < n; i++) {
      var dx = xs[i] - meanX;
      var dy = ys[i] - meanY;
      num += dx * dy;
      denX += dx * dx;
      denY += dy * dy;
    }

    return num / Math.Sqrt(denX * denY);
  }
}
