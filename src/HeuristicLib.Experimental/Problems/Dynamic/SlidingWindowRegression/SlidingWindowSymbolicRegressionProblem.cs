using HEAL.HeuristicLib.Data;
using HEAL.HeuristicLib.Encodings.SymbolicExpressions;
using HEAL.HeuristicLib.MachineLearning;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems.MachineLearning;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Problems.Dynamic.SlidingWindowRegression;

public class SlidingWindowSymbolicRegressionProblem
    : DynamicProblem<SlidingWindowSymbolicRegressionProblem, ExpressionTree, ExpressionTreeSearchSpace>
{
    private readonly SymbolicRegressionProblem innerProblem;
    private SymbolicRegressionProblem windowProblem;

    public SlidingWindowSymbolicRegressionProblem(SymbolicRegressionProblem problem, int windowStart = 0, int windowLength = 100, int stepSize = 10, UpdatePolicy updatePolicy = UpdatePolicy.AfterEvaluation, int epochLength = int.MaxValue)
        : base(problem.Objective, problem.SearchSpace, RandomNumberGenerator.Create(0), updatePolicy, epochLength)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(windowStart);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(windowLength);
        ArgumentOutOfRangeException.ThrowIfNegative(stepSize);

        innerProblem = problem;
        windowProblem = problem;
        StepSize = stepSize;
        WindowLength = windowLength;
        CurrentState = (windowStart, windowStart + windowLength);
        RebuildWindowData();
    }

    public int StepSize { get; }
    public int WindowLength { get; }

    public (int StartIndex, int EndIndex) CurrentState { get; private set; }

    public override ObjectiveVector Evaluate(ExpressionTree solution, IRandomNumberGenerator random, EvaluationTiming timing)
    {
        return windowProblem.Evaluate(solution);
    }

    protected override void Update()
    {
        CurrentState = (CurrentState.StartIndex + StepSize, CurrentState.EndIndex + StepSize);
        RebuildWindowData();
    }

    private void RebuildWindowData()
    {
        var source = innerProblem.TrainingData;
        var rowCount = source.RowCount;
        var start = CurrentState.StartIndex;
        var rows = new int[CurrentState.EndIndex - start];
        for (var k = 0; k < rows.Length; k++)
        {
            var row = (start + k) % rowCount;
            rows[k] = row < 0 ? row + rowCount : row;
        }

        var inputColumns = source.Inputs.Columns
            .OfType<Series<double>>()
            .Select(series => SelectRows(series, rows))
            .ToArray();
        var target = SelectRows(source.Target, rows);

        var windowData = new RegressionData(new DataFrame(inputColumns), target);
        windowProblem = new SymbolicRegressionProblem(windowData, innerProblem.PredictionMetrics, innerProblem.ExpressionMetrics, innerProblem.SearchSpace, innerProblem.UseLinearScaling, innerProblem.Objective.TotalOrderComparer);
    }

    private static Series<double> SelectRows(Series<double> source, IReadOnlyList<int> rows)
    {
        var sourceValues = source.Values.Span;
        var selectedValues = new double[rows.Count];
        for (var i = 0; i < rows.Count; i++)
            selectedValues[i] = sourceValues[rows[i]];

        return Series<double>.FromOwnedArray(source.Name, selectedValues);
    }
}
