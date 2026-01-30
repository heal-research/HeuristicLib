namespace HEAL.HeuristicLib.Problems.DataAnalysis.OnlineCalculators;

public static class AutoCorrelationCalculator {
  public static double[] Calculate(double[] values, out OnlineCalculatorError error) {
    if (values.Any(x => double.IsNaN(x) || double.IsInfinity(x))) {
      error = OnlineCalculatorError.InvalidValueAdded;
      return [];
    }

    error = OnlineCalculatorError.None;
    return CircularCrossCorrelation(values, values);
  }

  public static double[] CircularCrossCorrelation(double[] x, double[] y) {
    var n = Math.Max(x.Length, y.Length);
    var result = new double[n];

    for (var shift = 0; shift < n; shift++) {
      var sum = 0.0;
      for (var i = 0; i < n; i++) {
        var xi = x[i % x.Length];
        var yi = y[(i + shift) % y.Length];
        sum += xi * yi;
      }

      result[shift] = sum;
    }

    return result;
  }
}
