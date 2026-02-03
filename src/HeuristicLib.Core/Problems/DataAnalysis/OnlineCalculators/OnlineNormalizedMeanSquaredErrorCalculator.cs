#pragma warning disable S2178
namespace HEAL.HeuristicLib.Problems.DataAnalysis.OnlineCalculators;

public class OnlineNormalizedMeanSquaredErrorCalculator {
  private readonly OnlineMeanAndVarianceCalculator meanSquaredErrorCalculator;
  private readonly OnlineMeanAndVarianceCalculator originalVarianceCalculator;

  public double NormalizedMeanSquaredError {
    get {
      var var = originalVarianceCalculator.PopulationVariance;
      var m = meanSquaredErrorCalculator.Mean;
      return var > 0 ? m / var : 0.0;
    }
  }

  public OnlineNormalizedMeanSquaredErrorCalculator() {
    meanSquaredErrorCalculator = new OnlineMeanAndVarianceCalculator();
    originalVarianceCalculator = new OnlineMeanAndVarianceCalculator();
    Reset();
  }

  #region IOnlineCalculator Members
  public OnlineCalculatorError ErrorState => meanSquaredErrorCalculator.MeanErrorState | originalVarianceCalculator.PopulationVarianceErrorState;
  public double Value => NormalizedMeanSquaredError;

  public void Reset() {
    meanSquaredErrorCalculator.Reset();
    originalVarianceCalculator.Reset();
  }

  public void Add(double original, double estimated) {
    // no need to check for validity of values explicitly as it is checked in the meanAndVariance calculator anyway
    var error = estimated - original;
    meanSquaredErrorCalculator.Add(error * error);
    originalVarianceCalculator.Add(original);
  }
  #endregion

  public static double Calculate(IEnumerable<double> originalValues, IEnumerable<double> estimatedValues, out OnlineCalculatorError errorState) {
    using var originalEnumerator = originalValues.GetEnumerator();
    using var estimatedEnumerator = estimatedValues.GetEnumerator();
    var normalizedMseCalculator = new OnlineNormalizedMeanSquaredErrorCalculator();

    //needed because otherwise the normalizedMSECalculator is in ErrorState.InsufficientValuesAdded
    if (originalEnumerator.MoveNext() & estimatedEnumerator.MoveNext()) {
      var original = originalEnumerator.Current;
      var estimated = estimatedEnumerator.Current;
      normalizedMseCalculator.Add(original, estimated);
    }

    // always move forward both enumerators (do not use short-circuit evaluation!)
    while (originalEnumerator.MoveNext() & estimatedEnumerator.MoveNext()) {
      var original = originalEnumerator.Current;
      var estimated = estimatedEnumerator.Current;
      normalizedMseCalculator.Add(original, estimated);
      if (normalizedMseCalculator.ErrorState != OnlineCalculatorError.None) break;
    }

    // check if both enumerators are at the end to make sure both enumerations have the same length
    if (normalizedMseCalculator.ErrorState == OnlineCalculatorError.None &&
        (estimatedEnumerator.MoveNext() || originalEnumerator.MoveNext())) {
      throw new ArgumentException("Number of elements in originalValues and estimatedValues enumeration doesn't match.");
    }

    errorState = normalizedMseCalculator.ErrorState;
    return normalizedMseCalculator.NormalizedMeanSquaredError;
  }
}
