using HEAL.HeuristicLib.Encodings.SymbolicExpressionTree;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems.DataAnalysis;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Problems.Dynamic.SlidingWindowRegression;

public class SlidingWindowSymbolicRegressionProblem
  : DynamicProblem<SymbolicExpressionTree, SymbolicExpressionTreeEncoding> {
  public SlidingWindowSymbolicRegressionProblem(
    SymbolicRegressionProblem problem,
    int windowStart = 0,
    int windowLength = 100,
    int stepSize = 10,
    UpdatePolicy updatePolicy = UpdatePolicy.AfterEvaluation,
    int epochLength = int.MaxValue
  ) : base(NoRandomGenerator.Instance, updatePolicy: updatePolicy, epochLength: epochLength) {
    ArgumentOutOfRangeException.ThrowIfNegative(windowStart);
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(windowLength);
    ArgumentOutOfRangeException.ThrowIfNegative(stepSize);

    innerProblem = problem;
    StepSize = stepSize;
    WindowLength = windowLength;
    CurrentState = (windowStart, windowStart + windowLength);
    RebuildWindowCache();
  }

  private readonly SymbolicRegressionProblem innerProblem;

  public int StepSize { get; }
  public int WindowLength { get; }

  public (int StartIndex, int EndIndex) CurrentState { get; private set; }

  protected int[] CachedRows = [];
  protected double[] CachedTargets = [];

  public override SymbolicExpressionTreeEncoding SearchSpace => innerProblem.SearchSpace;
  public override Objective Objective => innerProblem.Objective;

  public override ObjectiveVector Evaluate(SymbolicExpressionTree solution, IRandomNumberGenerator random, EvaluationTiming timing) {
    return innerProblem.Evaluate(solution, CachedRows, CachedTargets);
  }

  protected override void Update() {
    CurrentState = (CurrentState.StartIndex + StepSize, CurrentState.EndIndex + StepSize);
    RebuildWindowCache();
  }

  private void RebuildWindowCache() {
    var trainingRange = innerProblem.ProblemData.Partitions[DataAnalysisProblemData.PartitionType.Training];
    int rowCount = innerProblem.ProblemData.Dataset.Rows;
    int rangeStart = trainingRange.Start.IsFromEnd ? rowCount - trainingRange.Start.Value : trainingRange.Start.Value;
    int rangeEnd = trainingRange.End.IsFromEnd ? rowCount - trainingRange.End.Value : trainingRange.End.Value;
    ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(rangeEnd, rangeStart);
    int rangeLength = rangeEnd - rangeStart;
    int start = CurrentState.StartIndex;

    var rows = new int[CurrentState.EndIndex - start];
    for (int k = 0; k < rows.Length; k++) {
      int r = (start + k) % rangeLength;
      int offset = r < 0 ? r + rangeLength : r;
      rows[k] = rangeStart + offset;
    }

    var targets = innerProblem.ProblemData.Dataset
                              .GetDoubleValues(innerProblem.ProblemData.TargetVariable, rows)
                              .ToArray();

    CachedRows = rows;
    CachedTargets = targets;
  }
}
