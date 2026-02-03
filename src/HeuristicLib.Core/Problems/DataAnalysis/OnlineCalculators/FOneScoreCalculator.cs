namespace HEAL.HeuristicLib.Problems.DataAnalysis.OnlineCalculators;

public static class FOneScoreCalculator
{
  public static double Calculate(IEnumerable<double> originalValues, IEnumerable<double> estimatedValues, out OnlineCalculatorError errorState)
  {
    var enumerable = originalValues as ICollection<double> ?? originalValues.ToArray();
    if (enumerable.Distinct().Skip(2).Any()) {
      // TODO: we could use ClassificationPerformanceMeasuresCalculator instead of the ConfusionMatrixCalculator below to handle multi-class problems
      throw new ArgumentException("F1 score can only be calculated for binary classification.");
    }

    var confusionMatrix = ConfusionMatrixCalculator.Calculate(enumerable, estimatedValues, out errorState);
    if (!errorState.Equals(OnlineCalculatorError.None)) {
      return double.NaN;
    }

    //only one class has been present => F1 score cannot be calculated
    if (confusionMatrix.GetLength(0) != 2 || confusionMatrix.GetLength(1) != 2) {
      return double.NaN;
    }

    return CalculateFOne(confusionMatrix);
  }

  private static double CalculateFOne(double[,] confusionMatrix)
  {
    var precision = confusionMatrix[0, 0] / (confusionMatrix[0, 0] + confusionMatrix[0, 1]);
    var recall = confusionMatrix[0, 0] / (confusionMatrix[0, 0] + confusionMatrix[1, 0]);

    return 2 * (precision * recall / (precision + recall));
  }
}
