using HEAL.HeuristicLib.Optimization;

#pragma warning disable S2178
namespace HEAL.HeuristicLib.Problems.DataAnalysis.OnlineCalculators;

public class OnlineMeanAbsolutePercentageErrorCalculator
{
  private int n;
  private double sre;

  public OnlineMeanAbsolutePercentageErrorCalculator() => Reset();
  public double MeanAbsolutePercentageError => n > 0 ? sre / n : 0.0;

  public static double Calculate(IEnumerable<double> originalValues, IEnumerable<double> estimatedValues, out OnlineCalculatorError errorState)
  {
    using var originalEnumerator = originalValues.GetEnumerator();
    using var estimatedEnumerator = estimatedValues.GetEnumerator();
    var calculator = new OnlineMeanAbsolutePercentageErrorCalculator();

    // always move forward both enumerators (do not use short-circuit evaluation!)
    while (originalEnumerator.MoveNext() & estimatedEnumerator.MoveNext()) {
      var original = originalEnumerator.Current;
      var estimated = estimatedEnumerator.Current;
      calculator.Add(original, estimated);
      if (calculator.ErrorState != OnlineCalculatorError.None) {
        break;
      }
    }

    // check if both enumerators are at the end to make sure both enumerations have the same length
    if (calculator.ErrorState == OnlineCalculatorError.None &&
        (estimatedEnumerator.MoveNext() || originalEnumerator.MoveNext())) {
      throw new ArgumentException("Number of elements in originalValues and second estimatedValues doesn't match.");
    }

    errorState = calculator.ErrorState;

    return calculator.MeanAbsolutePercentageError;
  }

  #region IOnlineCalculator Members

  public OnlineCalculatorError ErrorState { get; private set; }
  public double Value => MeanAbsolutePercentageError;

  public void Reset()
  {
    n = 0;
    sre = 0.0;
    ErrorState = OnlineCalculatorError.InsufficientElementsAdded;
  }

  public void Add(double original, double estimated)
  {
    if (double.IsNaN(estimated) || double.IsInfinity(estimated) ||
        double.IsNaN(original) || double.IsInfinity(original) ||
        original.IsAlmost(0.0)) {
      ErrorState |= OnlineCalculatorError.InvalidValueAdded;

      return;
    }

    sre += Math.Abs((estimated - original) / original);
    n++;
    ErrorState &= ~OnlineCalculatorError.InsufficientElementsAdded;// n >= 1
  }

  #endregion

}
