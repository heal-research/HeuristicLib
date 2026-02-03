#pragma warning disable S2178
namespace HEAL.HeuristicLib.Problems.DataAnalysis.OnlineCalculators;

public class OnlineTheilsUStatisticCalculator
{
  private readonly OnlineMeanAndVarianceCalculator squaredErrorMeanCalculator;
  private readonly OnlineMeanAndVarianceCalculator unbiasedEstimatorMeanCalculator;

  private OnlineCalculatorError errorState;

  public OnlineTheilsUStatisticCalculator()
  {
    squaredErrorMeanCalculator = new OnlineMeanAndVarianceCalculator();
    unbiasedEstimatorMeanCalculator = new OnlineMeanAndVarianceCalculator();
    Reset();
  }

  public double TheilsUStatistic => Math.Sqrt(squaredErrorMeanCalculator.Mean) / Math.Sqrt(unbiasedEstimatorMeanCalculator.Mean);
  public OnlineCalculatorError ErrorState => errorState | squaredErrorMeanCalculator.MeanErrorState | unbiasedEstimatorMeanCalculator.MeanErrorState;

  public static double Calculate(double startValue, IEnumerable<double> actualContinuation, IEnumerable<double> referenceContinuation, IEnumerable<double> predictedContinuation, out OnlineCalculatorError errorState)
  {
    var calculator = new OnlineTheilsUStatisticCalculator();
    calculator.Add(startValue, actualContinuation, referenceContinuation, predictedContinuation);
    errorState = calculator.ErrorState;

    return calculator.TheilsUStatistic;
  }

  public static double Calculate(IEnumerable<double> startValues, IEnumerable<IEnumerable<double>> actualContinuations, IEnumerable<IEnumerable<double>> referenceContinuations, IEnumerable<IEnumerable<double>> predictedContinuations, out OnlineCalculatorError errorState)
  {
    using var startValueEnumerator = startValues.GetEnumerator();
    using var actualContinuationsEnumerator = actualContinuations.GetEnumerator();
    using var referenceContinuationsEnumerator = referenceContinuations.GetEnumerator();
    using var predictedContinuationsEnumerator = predictedContinuations.GetEnumerator();

    var calculator = new OnlineTheilsUStatisticCalculator();

    // always move forward all enumerators (do not use short-circuit evaluation!)
    while (startValueEnumerator.MoveNext() & actualContinuationsEnumerator.MoveNext() & referenceContinuationsEnumerator.MoveNext() & predictedContinuationsEnumerator.MoveNext()) {
      calculator.Add(startValueEnumerator.Current, actualContinuationsEnumerator.Current, referenceContinuationsEnumerator.Current, predictedContinuationsEnumerator.Current);
      if (calculator.ErrorState != OnlineCalculatorError.None) {
        break;
      }
    }

    // check if all enumerators are at the end to make sure both enumerations have the same length
    if (calculator.ErrorState == OnlineCalculatorError.None &&
        (startValueEnumerator.MoveNext() || actualContinuationsEnumerator.MoveNext() || referenceContinuationsEnumerator.MoveNext() || predictedContinuationsEnumerator.MoveNext())) {
      throw new ArgumentException("Number of elements in startValues, actualContinuations, referenceContinuation and estimatedValues predictedContinuations doesn't match.");
    }

    errorState = calculator.ErrorState;

    return calculator.TheilsUStatistic;
  }

  #region IOnlineEvaluator Members

  public double Value => TheilsUStatistic;

  public void Add(double startValue, IEnumerable<double> actualContinuation, IEnumerable<double> referenceContinuation, IEnumerable<double> predictedContinuation)
  {
    if (double.IsNaN(startValue) || (errorState & OnlineCalculatorError.InvalidValueAdded) > 0) {
      errorState |= OnlineCalculatorError.InvalidValueAdded;

      return;
    }

    using var actualEnumerator = actualContinuation.GetEnumerator();
    using var predictedEnumerator = predictedContinuation.GetEnumerator();
    using var referenceEnumerator = referenceContinuation.GetEnumerator();
    while (actualEnumerator.MoveNext() & predictedEnumerator.MoveNext() & referenceEnumerator.MoveNext()
           & ErrorState != OnlineCalculatorError.InvalidValueAdded) {
      var actual = actualEnumerator.Current;
      var predicted = predictedEnumerator.Current;
      var reference = referenceEnumerator.Current;
      if (double.IsNaN(actual) || double.IsNaN(predicted) || double.IsNaN(reference)) {
        errorState |= OnlineCalculatorError.InvalidValueAdded;
      } else {
        // error of predicted change
        var errorPredictedChange = predicted - startValue - (actual - startValue);
        squaredErrorMeanCalculator.Add(errorPredictedChange * errorPredictedChange);

        var errorReference = reference - startValue - (actual - startValue);
        unbiasedEstimatorMeanCalculator.Add(errorReference * errorReference);
      }
    }

    // check if both enumerators are at the end to make sure both enumerations have the same length
    if (actualEnumerator.MoveNext() || predictedEnumerator.MoveNext() || referenceEnumerator.MoveNext()) {
      errorState |= OnlineCalculatorError.InvalidValueAdded;
    } else {
      errorState &= ~OnlineCalculatorError.InsufficientElementsAdded;// n >= 1
    }
  }

  public void Reset()
  {
    squaredErrorMeanCalculator.Reset();
    unbiasedEstimatorMeanCalculator.Reset();
    errorState = OnlineCalculatorError.InsufficientElementsAdded;
  }

  #endregion

}
