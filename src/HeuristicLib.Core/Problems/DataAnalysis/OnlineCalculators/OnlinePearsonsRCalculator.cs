using HEAL.HeuristicLib.Optimization;

#pragma warning disable S2178
namespace HEAL.HeuristicLib.Problems.DataAnalysis.OnlineCalculators;

public class OnlinePearsonsRCalculator
{
  private readonly OnlineCovarianceCalculator covCalculator = new();
  private readonly OnlineMeanAndVarianceCalculator sxCalculator = new();
  private readonly OnlineMeanAndVarianceCalculator syCalculator = new();

  public double R
  {
    get {
      var xVar = sxCalculator.PopulationVariance;
      var yVar = syCalculator.PopulationVariance;
      if (xVar.IsAlmost(0.0) || yVar.IsAlmost(0.0)) {
        return 0.0;
      }

      var r = covCalculator.Covariance / (Math.Sqrt(xVar) * Math.Sqrt(yVar));
      r = r switch {
        < -1.0 => -1.0,
        > 1.0 => 1.0,
        _ => r
      };
      return r;
    }
  }

  #region IOnlineCalculator Members

  public OnlineCalculatorError ErrorState => covCalculator.ErrorState | sxCalculator.PopulationVarianceErrorState | syCalculator.PopulationVarianceErrorState;
  public double Value => R;

  public void Reset()
  {
    covCalculator.Reset();
    sxCalculator.Reset();
    syCalculator.Reset();
  }

  public void Add(double x, double y)
  {
    // no need to check validity of values explicitly here as it is checked in all three evaluators 
    covCalculator.Add(x, y);
    sxCalculator.Add(x);
    syCalculator.Add(y);
  }

  #endregion

  public static double Calculate(IEnumerable<double> first, IEnumerable<double> second, out OnlineCalculatorError errorState)
  {
    using var firstEnumerator = first.GetEnumerator();
    using var secondEnumerator = second.GetEnumerator();
    var calculator = new OnlinePearsonsRCalculator();

    // always move forward both enumerators (do not use short-circuit evaluation!)
    while (firstEnumerator.MoveNext() & secondEnumerator.MoveNext()) {
      var original = firstEnumerator.Current;
      var estimated = secondEnumerator.Current;
      calculator.Add(original, estimated);
      if (calculator.ErrorState != OnlineCalculatorError.None) {
        break;
      }
    }

    // check if both enumerators are at the end to make sure both enumerations have the same length
    if (calculator.ErrorState == OnlineCalculatorError.None &&
        (secondEnumerator.MoveNext() || firstEnumerator.MoveNext())) {
      throw new ArgumentException("Number of elements in first and second enumeration doesn't match.");
    }

    errorState = calculator.ErrorState;
    return calculator.R;
  }
}
