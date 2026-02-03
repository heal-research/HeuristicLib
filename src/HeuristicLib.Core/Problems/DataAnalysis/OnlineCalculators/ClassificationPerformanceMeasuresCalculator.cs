using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.OnlineCalculators;

public class ClassificationPerformanceMeasuresCalculator {
  public ClassificationPerformanceMeasuresCalculator(string positiveClassName, double positiveClassValue) {
    PositiveClassName = positiveClassName;
    PositiveClassValue = positiveClassValue;
    Reset();
  }

  #region Properties
  private int truePositiveCount, falsePositiveCount, trueNegativeCount, falseNegativeCount;

  public string PositiveClassName { get; }

  public double PositiveClassValue { get; }
  public double TruePositiveRate {
    get {
      double divisor = truePositiveCount + falseNegativeCount;
      return divisor.IsAlmost(0) ? double.NaN : truePositiveCount / divisor;
    }
  }
  public double TrueNegativeRate {
    get {
      double divisor = falsePositiveCount + trueNegativeCount;
      return divisor.IsAlmost(0) ? double.NaN : trueNegativeCount / divisor;
    }
  }
  public double PositivePredictiveValue {
    get {
      double divisor = truePositiveCount + falsePositiveCount;
      return divisor.IsAlmost(0) ? double.NaN : truePositiveCount / divisor;
    }
  }
  public double NegativePredictiveValue {
    get {
      double divisor = trueNegativeCount + falseNegativeCount;
      return divisor.IsAlmost(0) ? double.NaN : trueNegativeCount / divisor;
    }
  }
  public double FalsePositiveRate {
    get {
      double divisor = falsePositiveCount + trueNegativeCount;
      return divisor.IsAlmost(0) ? double.NaN : falsePositiveCount / divisor;
    }
  }
  public double FalseDiscoveryRate {
    get {
      double divisor = falsePositiveCount + truePositiveCount;
      return divisor.IsAlmost(0) ? double.NaN : falsePositiveCount / divisor;
    }
  }

  public OnlineCalculatorError ErrorState { get; private set; }
  #endregion

  public void Reset() {
    truePositiveCount = 0;
    falseNegativeCount = 0;
    trueNegativeCount = 0;
    falseNegativeCount = 0;
    ErrorState = OnlineCalculatorError.InsufficientElementsAdded;
  }

  public void Add(double originalClassValue, double estimatedClassValue) {
    // ignore cases where original is NaN completely 
    if (double.IsNaN(originalClassValue)) return;

    if (originalClassValue.IsAlmost(PositiveClassValue)
        || estimatedClassValue.IsAlmost(PositiveClassValue)) { //positive class/positive class estimation
      if (estimatedClassValue.IsAlmost(originalClassValue)) {
        truePositiveCount++;
      } else {
        if (estimatedClassValue.IsAlmost(PositiveClassValue)) //misclassification of the negative class
          falsePositiveCount++;
        else //misclassification of the positive class
          falseNegativeCount++;
      }
    } else { //negative class/negative class estimation
      //In a multiclass classification all misclassifications of the negative class
      //will be treated as true negatives except on positive class estimations
      trueNegativeCount++;
    }

    ErrorState = OnlineCalculatorError.None; // number of (non-NaN) samples >= 1
  }

  public void Calculate(IEnumerable<double> originalClassValues, IEnumerable<double> estimatedClassValues) {
    using var originalEnumerator = originalClassValues.GetEnumerator();
    using var estimatedEnumerator = estimatedClassValues.GetEnumerator();

    // always move forward both enumerators (do not use short-circuit evaluation!)
    while (true) {
      var hasOriginal = originalEnumerator.MoveNext();
      var hasEstimated = estimatedEnumerator.MoveNext();
      if (!hasOriginal || !hasEstimated)
        break;

      var original = originalEnumerator.Current;
      var estimated = estimatedEnumerator.Current;
      Add(original, estimated);

      if (ErrorState != OnlineCalculatorError.None)
        break;
    }

    // check if both enumerators are at the end to make sure both enumerations have the same length
    if (ErrorState == OnlineCalculatorError.None && (estimatedEnumerator.MoveNext() || originalEnumerator.MoveNext())) {
      throw new ArgumentException("Number of elements in originalValues and estimatedValues enumerations doesn't match.");
    }
  }
}
