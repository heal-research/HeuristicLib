using HEAL.HeuristicLib.Optimization;

#pragma warning disable S2178
namespace HEAL.HeuristicLib.Problems.DataAnalysis.OnlineCalculators;

public class OnlineDirectionalSymmetryCalculator
{
  private int n;
  private int nCorrect;

  public OnlineDirectionalSymmetryCalculator() => Reset();

  public double DirectionalSymmetry => n < 1 ? 0.0 : (double)nCorrect / n;

  public double Value => DirectionalSymmetry;

  public OnlineCalculatorError ErrorState { get; private set; }

  public void Add(double startValue, IEnumerable<double> actualContinuation, IEnumerable<double> predictedContinuation)
  {
    if (double.IsNaN(startValue) || (ErrorState & OnlineCalculatorError.InvalidValueAdded) > 0) {
      ErrorState |= OnlineCalculatorError.InvalidValueAdded;
    } else {
      using var actualEnumerator = actualContinuation.GetEnumerator();
      using var predictedEnumerator = predictedContinuation.GetEnumerator();
      while (actualEnumerator.MoveNext() & predictedEnumerator.MoveNext() & (ErrorState != OnlineCalculatorError.InvalidValueAdded)) {
        var actual = actualEnumerator.Current;
        var predicted = predictedEnumerator.Current;
        if (double.IsNaN(actual) || double.IsNaN(predicted)) {
          ErrorState |= OnlineCalculatorError.InvalidValueAdded;
        } else {
          // count a prediction correct if the trend (positive/negative/no change) is predicted correctly
          if ((actual - startValue) * (predicted - startValue) > 0.0 ||
              (actual - startValue).IsAlmost(predicted - startValue)
             ) {
            nCorrect++;
          }

          n++;
        }
      }

      // check if both enumerators are at the end to make sure both enumerations have the same length
      if (actualEnumerator.MoveNext() || predictedEnumerator.MoveNext()) {
        ErrorState |= OnlineCalculatorError.InvalidValueAdded;
      } else {
        ErrorState &= ~OnlineCalculatorError.InsufficientElementsAdded;// n >= 1
      }
    }
  }

  public void Reset()
  {
    n = 0;
    nCorrect = 0;
    ErrorState = OnlineCalculatorError.InsufficientElementsAdded;
  }

  public static double Calculate(double startValue, IEnumerable<double> actualContinuation, IEnumerable<double> predictedContinuation, out OnlineCalculatorError errorState)
  {
    var dsCalculator = new OnlineDirectionalSymmetryCalculator();
    dsCalculator.Add(startValue, actualContinuation, predictedContinuation);
    errorState = dsCalculator.ErrorState;

    return dsCalculator.DirectionalSymmetry;
  }

  public static double Calculate(IEnumerable<double> startValues, IEnumerable<IEnumerable<double>> actualContinuations, IEnumerable<IEnumerable<double>> predictedContinuations, out OnlineCalculatorError errorState)
  {
    using var startValueEnumerator = startValues.GetEnumerator();
    using var actualContinuationsEnumerator = actualContinuations.GetEnumerator();
    using var predictedContinuationsEnumerator = predictedContinuations.GetEnumerator();
    var dsCalculator = new OnlineDirectionalSymmetryCalculator();

    // always move forward all enumerators (do not use short-circuit evaluation!)
    while (startValueEnumerator.MoveNext() & actualContinuationsEnumerator.MoveNext() & predictedContinuationsEnumerator.MoveNext()) {
      dsCalculator.Add(startValueEnumerator.Current, actualContinuationsEnumerator.Current, predictedContinuationsEnumerator.Current);
      if (dsCalculator.ErrorState != OnlineCalculatorError.None) {
        break;
      }
    }

    // check if all enumerators are at the end to make sure both enumerations have the same length
    if (dsCalculator.ErrorState == OnlineCalculatorError.None &&
        (startValueEnumerator.MoveNext() || actualContinuationsEnumerator.MoveNext() || predictedContinuationsEnumerator.MoveNext())) {
      throw new ArgumentException("Number of elements in startValues, actualContinuations and estimatedValues predictedContinuations doesn't match.");
    }

    errorState = dsCalculator.ErrorState;

    return dsCalculator.DirectionalSymmetry;
  }
}
