namespace HEAL.HeuristicLib.Problems.DataAnalysis.OnlineCalculators;

public static class MatthewsCorrelationCoefficientCalculator
{
  public static double Calculate(IEnumerable<double> originalValues, IEnumerable<double> estimatedValues, out OnlineCalculatorError errorState)
  {
    var confusionMatrix = ConfusionMatrixCalculator.Calculate(originalValues, estimatedValues, out errorState);

    return !errorState.Equals(OnlineCalculatorError.None) ? double.NaN : CalculateMcc(confusionMatrix);
  }

  private static double CalculateMcc(double[,] confusionMatrix)
  {
    if (confusionMatrix.GetLength(0) != confusionMatrix.GetLength(1)) {
      throw new ArgumentException("Confusion matrix is not a square matrix.");
    }

    var classes = confusionMatrix.GetLength(0);
    double numerator = 0;
    for (var k = 0; k < classes; k++) {
      for (var l = 0; l < classes; l++) {
        for (var m = 0; m < classes; m++) {
          numerator += confusionMatrix[k, k] * confusionMatrix[m, l] - confusionMatrix[l, k] * confusionMatrix[k, m];
        }
      }
    }

    double denominator1 = 0;
    double denominator2 = 0;
    for (var k = 0; k < classes; k++) {
      double clk = 0;
      double cgf = 0;
      double ckl = 0;
      double cfg = 0;
      for (var l = 0; l < classes; l++) {
        clk += confusionMatrix[l, k];
        ckl += confusionMatrix[k, l];
      }

      for (var f = 0; f < classes; f++) {
        if (f == k) {
          continue;
        }

        for (var g = 0; g < classes; g++) {
          cgf += confusionMatrix[g, f];
          cfg += confusionMatrix[f, g];
        }
      }

      denominator1 += clk * cgf;
      denominator2 += ckl * cfg;
    }

    denominator1 = Math.Sqrt(denominator1);
    denominator2 = Math.Sqrt(denominator2);

    return numerator / (denominator1 * denominator2);
  }
}
