using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems.DataAnalysis;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Trees;

namespace HEAL.HeuristicLib.Problems.Dynamic.SlidingWindowRegression;

public class SlidingWindowSymbolicRegressionProblem
  : DynamicProblem<SymbolicExpressionTree, SymbolicExpressionTreeSearchSpace>
{
  private readonly SymbolicRegressionProblem innerProblem;

  protected int[] CachedRows = [];
  protected double[] CachedTargets = [];

  public SlidingWindowSymbolicRegressionProblem(
    SymbolicRegressionProblem problem,
    int windowStart = 0,
    int windowLength = 100,
    int stepSize = 10,
    UpdatePolicy updatePolicy = UpdatePolicy.AfterEvaluation,
    int epochLength = int.MaxValue
  ) : base(RandomNumberGenerator.Create(0), updatePolicy, epochLength)
  {
    ArgumentOutOfRangeException.ThrowIfNegative(windowStart);
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(windowLength);
    ArgumentOutOfRangeException.ThrowIfNegative(stepSize);

    innerProblem = problem;
    StepSize = stepSize;
    WindowLength = windowLength;
    CurrentState = (windowStart, windowStart + windowLength);
    RebuildWindowCache();
  }

  public int StepSize { get; }
  public int WindowLength { get; }

  public (int StartIndex, int EndIndex) CurrentState { get; private set; }

  public override SymbolicExpressionTreeSearchSpace SearchSpace => innerProblem.SearchSpace;
  public override Objective Objective => innerProblem.Objective;

  public override ObjectiveVector Evaluate(SymbolicExpressionTree solution, IRandomNumberGenerator random, EvaluationTiming timing) => innerProblem.Evaluate(solution, CachedRows, CachedTargets);

  protected override void Update()
  {
    CurrentState = (CurrentState.StartIndex + StepSize, CurrentState.EndIndex + StepSize);
    RebuildWindowCache();
  }

  private void RebuildWindowCache()
  {
    var trainingRange = innerProblem.ProblemData.Partitions[DataAnalysisProblemData.PartitionType.Training];
    var rowCount = innerProblem.ProblemData.Dataset.Rows;
    var rangeStart = trainingRange.Start.IsFromEnd ? rowCount - trainingRange.Start.Value : trainingRange.Start.Value;
    var rangeEnd = trainingRange.End.IsFromEnd ? rowCount - trainingRange.End.Value : trainingRange.End.Value;
    ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(rangeEnd, rangeStart);
    var rangeLength = rangeEnd - rangeStart;
    var start = CurrentState.StartIndex;

    var rows = new int[CurrentState.EndIndex - start];
    for (var k = 0; k < rows.Length; k++) {
      var r = (start + k) % rangeLength;
      var offset = r < 0 ? r + rangeLength : r;
      rows[k] = rangeStart + offset;
    }

    var targets = innerProblem.ProblemData.Dataset
                              .GetDoubleValues(innerProblem.ProblemData.TargetVariable, rows)
                              .ToArray();

    CachedRows = rows;
    CachedTargets = targets;
  }
}
