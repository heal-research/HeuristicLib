using HEAL.HeuristicLib.Problems.DataAnalysis;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression;

namespace HEAL.HeuristicLib.Problems.Dynamic;

public class SlidingWindowRegressionProblemData(
  Dataset dataset,
  IEnumerable<string> allowedInputVariables,
  string targetVariable,
  int startIndex,
  int endIndex)
  : RegressionProblemData(dataset, allowedInputVariables, targetVariable) {
  public int StartIndex { get; init; } = startIndex;
  public int EndIndex { get; init; } = endIndex;
}
