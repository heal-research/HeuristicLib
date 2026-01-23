using HEAL.HeuristicLib.Optimization;

#pragma warning disable S2178
namespace HEAL.HeuristicLib.Problems.DataAnalysis.OnlineCalculators;

public class OnlineWeightedClassificationMeanSquaredErrorCalculator
{
  private double sse;
  private int n;
  public double WeightedResidualsMeanSquaredError => n > 0 ? sse / n : 0.0;

  public double PositiveClassValue { get; }
  public double ClassValuesMax { get; }
  public double ClassValuesMin { get; }
  public double DefiniteResidualsWeight { get; }
  public double PositiveClassResidualsWeight { get; }
  public double NegativeClassesResidualsWeight { get; }

  public OnlineWeightedClassificationMeanSquaredErrorCalculator(double positiveClassValue, double classValuesMax, double classValuesMin,
    double definiteResidualsWeight, double positiveClassResidualsWeight, double negativeClassesResidualsWeight)
  {
    PositiveClassValue = positiveClassValue;
    ClassValuesMax = classValuesMax;
    ClassValuesMin = classValuesMin;
    DefiniteResidualsWeight = definiteResidualsWeight;
    PositiveClassResidualsWeight = positiveClassResidualsWeight;
    NegativeClassesResidualsWeight = negativeClassesResidualsWeight;
    Reset();
  }

  #region IOnlineCalculator Members

  public OnlineCalculatorError ErrorState { get; private set; }
  public double Value => WeightedResidualsMeanSquaredError;

  public void Reset()
  {
    n = 0;
    sse = 0.0;
    ErrorState = OnlineCalculatorError.InsufficientElementsAdded;
  }

  public void Add(double original, double estimated)
  {
    if (double.IsNaN(estimated) || double.IsInfinity(estimated) ||
        double.IsNaN(original) || double.IsInfinity(original) || (ErrorState & OnlineCalculatorError.InvalidValueAdded) > 0) {
      ErrorState |= OnlineCalculatorError.InvalidValueAdded;
    } else {
      var error = estimated - original;
      double weight;
      //apply weight
      if (estimated > ClassValuesMax || estimated < ClassValuesMin) {
        weight = DefiniteResidualsWeight;
      } else if (original.IsAlmost(PositiveClassValue)) {
        weight = PositiveClassResidualsWeight;
      } else {
        weight = NegativeClassesResidualsWeight;
      }

      sse += error * error * weight;
      n++;
      ErrorState &= ~OnlineCalculatorError.InsufficientElementsAdded; // n >= 1
    }
  }

  #endregion

  public static double Calculate(IEnumerable<double> originalValues, IEnumerable<double> estimatedValues, double positiveClassValue, double classValuesMax, double classValuesMin,
    double definiteResidualsWeight, double positiveClassResidualsWeight, double negativeClassesResidualsWeight, out OnlineCalculatorError errorState)
  {
    using var originalEnumerator = originalValues.GetEnumerator();
    using var estimatedEnumerator = estimatedValues.GetEnumerator();
    var calculator = new OnlineWeightedClassificationMeanSquaredErrorCalculator(positiveClassValue, classValuesMax, classValuesMin, definiteResidualsWeight, positiveClassResidualsWeight, negativeClassesResidualsWeight);

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
      throw new ArgumentException("Number of elements in originalValues and estimatedValues enumerations doesn't match.");
    }

    errorState = calculator.ErrorState;
    return calculator.WeightedResidualsMeanSquaredError;
  }
}
