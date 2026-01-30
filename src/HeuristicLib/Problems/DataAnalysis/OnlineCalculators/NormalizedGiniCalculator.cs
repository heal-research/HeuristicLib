namespace HEAL.HeuristicLib.Problems.DataAnalysis.OnlineCalculators;

public static class NormalizedGiniCalculator {
  public static double Calculate(IEnumerable<double> originalValues, IEnumerable<double> estimatedValues, out OnlineCalculatorError errorState) {
    var originalValuesArr = originalValues.ToArray();
    var estimatedValuesArr = estimatedValues.ToArray();
    if (originalValuesArr.Length != estimatedValuesArr.Length) {
      throw new ArgumentException("Number of elements in originalValues and estimatedValues enumerations doesn't match.");
    }

    var oe = Gini(originalValuesArr, estimatedValuesArr, out errorState);
    if (errorState != OnlineCalculatorError.None)
      return double.NaN;

    return oe / Gini(originalValuesArr, originalValuesArr, out errorState);
  }

  private static double Gini(IEnumerable<double> original, IEnumerable<double> estimated, out OnlineCalculatorError errorState) {
    var pairs = estimated.Zip(original, (e, o) => (e, o)).OrderByDescending(p => p.e);
    errorState = OnlineCalculatorError.InsufficientElementsAdded;
    var giniSum = 0.0;
    var sumOriginal = 0.0;
    var n = 0;
    foreach (var p in pairs) {
      if (double.IsNaN(p.o) || double.IsNaN(p.e)) {
        errorState = OnlineCalculatorError.InvalidValueAdded;
        return double.NaN;
      }

      sumOriginal += p.o;
      giniSum += sumOriginal;
      n++;
    }

    if (n > 0) errorState = OnlineCalculatorError.None;
    giniSum /= sumOriginal;
    return (giniSum - (n + 1) / 2.0) / n;
  }
}
