using HEAL.HeuristicLib.Encodings.SymbolicExpressionTree;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems.DataAnalysis;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Problems.Dynamic.SlidingWindowRegression;

public class SlidingWindowSymbolicRegressionProblem : DynamicProblem<SymbolicExpressionTree, SymbolicExpressionTreeEncoding> {
  public SlidingWindowSymbolicRegressionProblem(SymbolicRegressionProblem problem,
                                                int windowStart = 0,
                                                int windowLength = 100,
                                                int stepSize = 10,
                                                UpdatePolicy updatePolicy = UpdatePolicy.AfterEvaluation,
                                                int epochLength = int.MaxValue) : base(updatePolicy, epochLength) {
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(windowLength);
    innerProblem = problem;
    CurrentState = (windowStart, windowStart + windowLength);
    StepSize = stepSize;
  }

  private readonly SymbolicRegressionProblem innerProblem;
  private int StepSize { get; }

  public (int StartIndex, int EndIndex) CurrentState { get; set; }

  public override SymbolicExpressionTreeEncoding SearchSpace => innerProblem.SearchSpace;
  public override Objective Objective => innerProblem.Objective;

  public override ObjectiveVector Evaluate(SymbolicExpressionTree solution, IRandomNumberGenerator random, EvaluationTiming timing) {
    var dataPartition = innerProblem.ProblemData.Partitions[DataAnalysisProblemData.PartitionType.Training];
    var targets = innerProblem.ProblemData.Dataset.GetDoubleValues(innerProblem.ProblemData.TargetVariable, RoundRobinRange(dataPartition)).ToArray();
    return innerProblem.Evaluate(solution, RoundRobinRange(dataPartition), targets);
  }

  protected override void Update() {
    CurrentState = (CurrentState.StartIndex + StepSize, CurrentState.EndIndex + StepSize);
  }

  private IEnumerable<int> RoundRobinRange(Range range) {
    var min = CurrentState.StartIndex;
    var max = CurrentState.EndIndex;
    // Extract start and end from the Range
    var (rangeStart, rangeEnd) = GetRangeBounds(range, max); // using max as array length fallback
    ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(rangeStart, rangeEnd);

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
