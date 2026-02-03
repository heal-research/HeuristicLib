#pragma warning disable S2178
namespace HEAL.HeuristicLib.Problems.DataAnalysis.OnlineCalculators;

public class OnlineBoundedMeanSquaredErrorCalculator
{
  private double errorSum;
  private int n;

  public OnlineBoundedMeanSquaredErrorCalculator(double lowerBound, double upperBound)
  {
    LowerBound = lowerBound;
    UpperBound = upperBound;
    Reset();
  }
  public double BoundedMeanSquaredError => n > 0 ? errorSum / n : 0.0;

  public double LowerBound { get; }
  public double UpperBound { get; }

  public static double Calculate(IEnumerable<double> originalValues, IEnumerable<double> estimatedValues, double lowerBound, double upperBound, out OnlineCalculatorError errorState)
  {
    using var originalEnumerator = originalValues.GetEnumerator();
    using var estimatedEnumerator = estimatedValues.GetEnumerator();
    var boundedMseCalculator = new OnlineBoundedMeanSquaredErrorCalculator(lowerBound, upperBound);

    // always move forward both enumerators (do not use short-circuit evaluation!)
    while (originalEnumerator.MoveNext() & estimatedEnumerator.MoveNext()) {
      var original = originalEnumerator.Current;
      var estimated = estimatedEnumerator.Current;
      boundedMseCalculator.Add(original, estimated);
      if (boundedMseCalculator.ErrorState != OnlineCalculatorError.None) {
        break;
      }
    }

    // check if both enumerators are at the end to make sure both enumerations have the same length
    if (boundedMseCalculator.ErrorState == OnlineCalculatorError.None &&
        (estimatedEnumerator.MoveNext() || originalEnumerator.MoveNext())) {
      throw new ArgumentException("Number of elements in originalValues and estimatedValues enumerations doesn't match.");
    }

    errorState = boundedMseCalculator.ErrorState;

    return boundedMseCalculator.BoundedMeanSquaredError;
  }

  #region IOnlineCalculator Members

  public OnlineCalculatorError ErrorState { get; private set; }
  public double Value => BoundedMeanSquaredError;

  public void Reset()
  {
    n = 0;
    errorSum = 0.0;
    ErrorState = OnlineCalculatorError.InsufficientElementsAdded;
  }

  public void Add(double original, double estimated)
  {
    if (double.IsNaN(estimated) || double.IsInfinity(estimated) ||
        double.IsNaN(original) || double.IsInfinity(original) || (ErrorState & OnlineCalculatorError.InvalidValueAdded) > 0) {
      ErrorState |= OnlineCalculatorError.InvalidValueAdded;

      return;
    }

    var error = estimated - original;
    if (estimated < LowerBound || estimated > UpperBound) {
      errorSum += Math.Abs(error);
    } else {
      errorSum += error * error;
    }
    n++;
    ErrorState &= ~OnlineCalculatorError.InsufficientElementsAdded;// n >= 1
  }

  #endregion

}
