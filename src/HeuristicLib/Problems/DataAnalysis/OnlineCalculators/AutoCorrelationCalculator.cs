namespace HEAL.HeuristicLib.Problems.DataAnalysis.OnlineCalculators;

public class AutoCorrelationCalculator {
  public static double[] Calculate(double[] values, out OnlineCalculatorError error) {
    if (values.Any(x => double.IsNaN(x) || double.IsInfinity(x))) {
      error = OnlineCalculatorError.InvalidValueAdded;
      return [];
    }

    error = OnlineCalculatorError.None;
    return CircularCrossCorrelation(values, values);
  }

  public static double[] CircularCrossCorrelation(double[] x, double[] y) {
    int n = Math.Max(x.Length, y.Length);
    double[] result = new double[n];

    for (int shift = 0; shift < n; shift++) {
      double sum = 0.0;
      for (int i = 0; i < n; i++) {
        double xi = x[i % x.Length];
        double yi = y[(i + shift) % y.Length];
        sum += xi * yi;
      }

      result[shift] = sum;
    }

    return result;
  }
}
