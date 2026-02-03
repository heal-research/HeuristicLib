namespace HEAL.HeuristicLib.Problems.DataAnalysis.OnlineCalculators;
#pragma warning disable S2178
public class OnlineMeanAbsoluteErrorCalculator
{
  private int n;
  private double sae;

  public OnlineMeanAbsoluteErrorCalculator() => Reset();
  public double MeanAbsoluteError => n > 0 ? sae / n : 0.0;

  public static double Calculate(IEnumerable<double> originalValues, IEnumerable<double> estimatedValues, out OnlineCalculatorError errorState)
  {
    using var originalEnumerator = originalValues.GetEnumerator();
    using var estimatedEnumerator = estimatedValues.GetEnumerator();
    var maeCalculator = new OnlineMeanAbsoluteErrorCalculator();

    // always move forward both enumerators (do not use short-circuit evaluation!)
    while (originalEnumerator.MoveNext() & estimatedEnumerator.MoveNext()) {
      var original = originalEnumerator.Current;
      var estimated = estimatedEnumerator.Current;
      maeCalculator.Add(original, estimated);
      if (maeCalculator.ErrorState != OnlineCalculatorError.None) {
        break;
      }
    }

    // check if both enumerators are at the end to make sure both enumerations have the same length
    if (maeCalculator.ErrorState == OnlineCalculatorError.None &&
        (estimatedEnumerator.MoveNext() || originalEnumerator.MoveNext())) {
      throw new ArgumentException("Number of elements in originalValues and estimatedValues enumerations doesn't match.");
    }

    errorState = maeCalculator.ErrorState;

    return maeCalculator.MeanAbsoluteError;
  }

  #region IOnlineCalculator Members

  public OnlineCalculatorError ErrorState { get; private set; }
  public double Value => MeanAbsoluteError;

  public void Reset()
  {
    n = 0;
    sae = 0.0;
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
    sae += Math.Abs(error);
    n++;
    ErrorState &= ~OnlineCalculatorError.InsufficientElementsAdded;// n >= 1
  }

  #endregion

}
