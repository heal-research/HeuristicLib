using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.OnlineCalculators;
#pragma warning disable S2178
public class OnlineAccuracyCalculator
{
  private int correctlyClassified;
  private int n;

  public OnlineAccuracyCalculator() => Reset();
  public double Accuracy => correctlyClassified / (double)n;

  public static double Calculate(IEnumerable<double> originalValues, IEnumerable<double> estimatedValues, out OnlineCalculatorError errorState)
  {
    using var originalEnumerator = originalValues.GetEnumerator();
    using var estimatedEnumerator = estimatedValues.GetEnumerator();
    var accuracyCalculator = new OnlineAccuracyCalculator();

    // always move forward both enumerators (do not use short-circuit evaluation!)
    while (originalEnumerator.MoveNext() & estimatedEnumerator.MoveNext()) {
      var original = originalEnumerator.Current;
      var estimated = estimatedEnumerator.Current;
      accuracyCalculator.Add(original, estimated);
      if (accuracyCalculator.ErrorState != OnlineCalculatorError.None) {
        break;
      }
    }

    // check if both enumerators are at the end to make sure both enumerations have the same length
    if (accuracyCalculator.ErrorState == OnlineCalculatorError.None &&
        (estimatedEnumerator.MoveNext() || originalEnumerator.MoveNext())) {
      throw new ArgumentException("Number of elements in originalValues and estimatedValues enumerations doesn't match.");
    }

    errorState = accuracyCalculator.ErrorState;

    return accuracyCalculator.Accuracy;
  }

  #region IOnlineCalculator Members

  public OnlineCalculatorError ErrorState { get; private set; }
  public double Value => Accuracy;

  public void Reset()
  {
    n = 0;
    correctlyClassified = 0;
    ErrorState = OnlineCalculatorError.InsufficientElementsAdded;
  }

  public void Add(double original, double estimated)
  {
    // ignore cases where original is NaN completely 
    if (double.IsNaN(original)) {
      return;
    }

    // increment number of observed samples
    n++;

    // original = estimated = +Inf counts as correctly classified
    // original = estimated = -Inf counts as correctly classified
    if (original.IsAlmost(estimated)) {
      correctlyClassified++;
    }

    ErrorState = OnlineCalculatorError.None;// number of (non-NaN) samples >= 1
  }

  #endregion

}
