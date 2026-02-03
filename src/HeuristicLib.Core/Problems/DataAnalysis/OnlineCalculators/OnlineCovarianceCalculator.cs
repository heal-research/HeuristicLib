#pragma warning disable S2178
namespace HEAL.HeuristicLib.Problems.DataAnalysis.OnlineCalculators;

public class OnlineCovarianceCalculator
{
  private int n;
  private double xMean, yMean, cn;

  public OnlineCovarianceCalculator() => Reset();
  public double Covariance => n > 0 ? cn / n : 0.0;

  public static double Calculate(IEnumerable<double> first, IEnumerable<double> second, out OnlineCalculatorError errorState)
  {
    using var firstEnumerator = first.GetEnumerator();
    using var secondEnumerator = second.GetEnumerator();
    var covarianceCalculator = new OnlineCovarianceCalculator();

    // always move forward both enumerators (do not use short-circuit evaluation!)
    while (firstEnumerator.MoveNext() & secondEnumerator.MoveNext()) {
      var x = secondEnumerator.Current;
      var y = firstEnumerator.Current;
      covarianceCalculator.Add(x, y);
      if (covarianceCalculator.ErrorState != OnlineCalculatorError.None) {
        break;
      }
    }

    // check if both enumerators are at the end to make sure both enumerations have the same length
    if (covarianceCalculator.ErrorState == OnlineCalculatorError.None &&
        (secondEnumerator.MoveNext() || firstEnumerator.MoveNext())) {
      throw new ArgumentException("Number of elements in first and second enumeration doesn't match.");
    }

    errorState = covarianceCalculator.ErrorState;

    return covarianceCalculator.Covariance;
  }

  #region IOnlineCalculator Members

  public OnlineCalculatorError ErrorState { get; private set; }
  public double Value => Covariance;

  public void Reset()
  {
    n = 0;
    cn = 0.0;
    xMean = 0.0;
    yMean = 0.0;
    ErrorState = OnlineCalculatorError.InsufficientElementsAdded;
  }

  public void Add(double x, double y)
  {
    if (double.IsNaN(y) || double.IsInfinity(y) || double.IsNaN(x) || double.IsInfinity(x) || (ErrorState & OnlineCalculatorError.InvalidValueAdded) > 0) {
      ErrorState |= OnlineCalculatorError.InvalidValueAdded;
    } else {
      n++;
      ErrorState &= ~OnlineCalculatorError.InsufficientElementsAdded;// n >= 1

      // online calculation of tMean
      xMean += (x - xMean) / n;
      var delta = y - yMean;// delta = (y - yMean(n-1))
      yMean += delta / n;

      // online calculation of covariance
      cn += delta * (x - xMean);// C(n) = C(n-1) + (y - yMean(n-1)) (t - tMean(n))       
    }
  }

  #endregion

}
