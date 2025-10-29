using HEAL.HeuristicLib.Encodings.SymbolicExpression;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems.DataAnalysis;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression;
using HEAL.HeuristicLib.Problems.DataAnalysis.Symbolic;

namespace HEAL.HeuristicLib.Problems.Dynamic;

public class SlidingWindowSymbolicRegressionProblem(
  SlidingWindowRegressionProblemData data,
  ICollection<IRegressionEvaluator<SymbolicExpressionTree>> objective,
  IComparer<ObjectiveVector> a,
  SymbolicExpressionTreeEncoding encoding,
  ISymbolicDataAnalysisExpressionTreeInterpreter interpreter,
  int parameterOptimizationIterations = -1)
  : SymbolicRegressionProblem(data, objective, a, encoding, interpreter, parameterOptimizationIterations),
    IStatefulProblem<SymbolicExpressionTree, SymbolicExpressionTreeEncoding, SlidingWindowUpdate, SlidingWindowRegressionProblemData> {
  public SlidingWindowRegressionProblemData CurrentState { get; set; } = data;

  public void UpdateState(SlidingWindowUpdate update) {
    CurrentState = new SlidingWindowRegressionProblemData(
      CurrentState.Dataset,
      CurrentState.InputVariables,
      CurrentState.TargetVariable,
      CurrentState.StartIndex + update.StepSize,
      CurrentState.EndIndex + update.StepSize);
  }

  public override RegressionProblemData ProblemData => CurrentState;

  public override ObjectiveVector Evaluate(SymbolicExpressionTree solution) {
    var dataPartition = ProblemData.Partitions[DataAnalysisProblemData.PartitionType.Training];
    var rows = RoundRobinRange(CurrentState.StartIndex, CurrentState.EndIndex, dataPartition);
    var targets = ProblemData.Dataset.GetDoubleValues(ProblemData.TargetVariable,
                               RoundRobinRange(CurrentState.StartIndex, CurrentState.EndIndex, dataPartition))
                             .ToArray();
    return Evaluate(solution, rows, targets);
  }

  public static IEnumerable<int> RoundRobinRange(int min, int max, Range range) {
    // Extract start and end from the Range
    var (rangeStart, rangeEnd) = GetRangeBounds(range, max); // using max as array length fallback
    ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(rangeStart, rangeEnd);
    ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(min, max);

    var rangeLength = rangeEnd - rangeStart;
    for (var i = min; i < max; i++) {
      yield return rangeStart + i % rangeLength;
    }
  }

  private static (int Start, int End) GetRangeBounds(Range range, int length) {
    var start = range.Start.IsFromEnd ? length - range.Start.Value : range.Start.Value;
    var end = range.End.IsFromEnd ? length - range.End.Value : range.End.Value;
    return (start, end);
  }
}
