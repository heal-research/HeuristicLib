namespace HEAL.HeuristicLib.Problems.DataAnalysis.OnlineCalculators;

[Flags]
public enum OnlineCalculatorError
{
  /// <summary>
  ///   No error occurred
  /// </summary>
  None = 0,

  /// <summary>
  ///   An invalid value has been added (often +/- Infinity and NaN are invalid values)
  /// </summary>
  InvalidValueAdded = 1,

  /// <summary>
  ///   The number of elements added to the DirectEvaluator is not sufficient to calculate the result value
  /// </summary>
  InsufficientElementsAdded = 2
}
