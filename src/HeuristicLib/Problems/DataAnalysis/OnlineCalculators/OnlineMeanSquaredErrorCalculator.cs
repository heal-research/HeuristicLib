#pragma warning disable S2178
namespace HEAL.HeuristicLib.Problems.DataAnalysis.OnlineCalculators;

public class OnlineMeanSquaredErrorCalculator {
  private double sse;
  private int n;
  public double MeanSquaredError => n > 0 ? sse / n : 0.0;

  public OnlineMeanSquaredErrorCalculator() {
    Reset();
  }

  #region IOnlineCalculator Members
  public OnlineCalculatorError ErrorState { get; private set; }
  public double Value => MeanSquaredError;

  public void Reset() {
    n = 0;
    sse = 0.0;
    ErrorState = OnlineCalculatorError.InsufficientElementsAdded;
  }

  public void Add(double original, double estimated) {
    if (double.IsNaN(estimated) || double.IsInfinity(estimated) ||
        double.IsNaN(original) || double.IsInfinity(original) || (ErrorState & OnlineCalculatorError.InvalidValueAdded) > 0) {
      ErrorState |= OnlineCalculatorError.InvalidValueAdded;
      return;
    }

    var error = estimated - original;
    sse += error * error;
    n++;
    ErrorState &= ~OnlineCalculatorError.InsufficientElementsAdded; // n >= 1
  }
  #endregion

  public static double Calculate(IEnumerable<double> originalValues, IEnumerable<double> estimatedValues, out OnlineCalculatorError errorState) {
    using var originalEnumerator = originalValues.GetEnumerator();
    using var estimatedEnumerator = estimatedValues.GetEnumerator();
    var mseCalculator = new OnlineMeanSquaredErrorCalculator();

    // always move forward both enumerators (do not use short-circuit evaluation!)
    while (originalEnumerator.MoveNext() & estimatedEnumerator.MoveNext()) {
      var original = originalEnumerator.Current;
      var estimated = estimatedEnumerator.Current;
      mseCalculator.Add(original, estimated);
      if (mseCalculator.ErrorState != OnlineCalculatorError.None) break;
    }

    // check if both enumerators are at the end to make sure both enumerations have the same length
    if (mseCalculator.ErrorState == OnlineCalculatorError.None &&
        (estimatedEnumerator.MoveNext() || originalEnumerator.MoveNext())) {
      throw new ArgumentException("Number of elements in originalValues and estimatedValues enumerations doesn't match.");
    }

    errorState = mseCalculator.ErrorState;
    return mseCalculator.MeanSquaredError;
  }
}
