using HEAL.HeuristicLib.DataAnalysis.Regression;
using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.SymbolicExpressions;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Regression;

public sealed class SymbolicRegressionProblem
    : SingleSolutionProblem<ExpressionTree, ExpressionTreeSearchSpace>
{
    public SymbolicRegressionProblem(RegressionData trainingData, ExpressionTreeSearchSpace searchSpace)
        : this(trainingData, [Metrics.MSE], [], searchSpace)
    {
    }

    public SymbolicRegressionProblem(RegressionData trainingData, IRegressionMetric metric, ExpressionTreeSearchSpace searchSpace)
        : this(trainingData, [metric], [], searchSpace)
    {
    }

    public SymbolicRegressionProblem(RegressionData trainingData, IEnumerable<IRegressionMetric> predictionMetrics, IEnumerable<IExpressionMetric> expressionMetrics, ExpressionTreeSearchSpace searchSpace, IComparer<ObjectiveVector>? totalOrderComparer = null)
        : this(trainingData, predictionMetrics.ToImmutableArray(), expressionMetrics.ToImmutableArray(), searchSpace, totalOrderComparer)
    {
    }

    private SymbolicRegressionProblem(RegressionData trainingData, ImmutableArray<IRegressionMetric> predictionMetrics, ImmutableArray<IExpressionMetric> expressionMetrics, ExpressionTreeSearchSpace searchSpace, IComparer<ObjectiveVector>? totalOrderComparer)
        : base(CreateObjective(predictionMetrics, expressionMetrics, totalOrderComparer), searchSpace)
    {
        ValidateVariables(trainingData, searchSpace);
        TrainingData = trainingData;
        PredictionMetrics = predictionMetrics;
        ExpressionMetrics = expressionMetrics;
    }

    public RegressionData TrainingData { get; }
    public ImmutableArray<IRegressionMetric> PredictionMetrics { get; }
    public ImmutableArray<IExpressionMetric> ExpressionMetrics { get; }

    public ObjectiveVector Evaluate(ExpressionTree expression)
    {
        var values = new double[PredictionMetrics.Length + ExpressionMetrics.Length];

        if (PredictionMetrics.Length > 0)
        {
            var predictions = expression.Evaluate(TrainingData.Inputs);
            var targets = TrainingData.Target.Values.Span;
            for (var i = 0; i < PredictionMetrics.Length; i++)
                values[i] = PredictionMetrics[i].Evaluate(predictions, targets);
        }

        for (var i = 0; i < ExpressionMetrics.Length; i++)
            values[PredictionMetrics.Length + i] = ExpressionMetrics[i].Evaluate(expression);

        return new ObjectiveVector(values);
    }

    public override ObjectiveVector Evaluate(ExpressionTree expression, IRandomNumberGenerator random) =>
        Evaluate(expression);

    private static void ValidateVariables(RegressionData trainingData, ExpressionTreeSearchSpace searchSpace)
    {
        foreach (var variableName in searchSpace.Symbols
                     .OfType<VariableSymbol>()
                     .SelectMany(symbol => symbol.Variables)
                     .Distinct(StringComparer.Ordinal))
        {
            if (!trainingData.Inputs.TryGet<double>(variableName, out _))
            {
                throw new ArgumentException(
                    $"The search-space variable '{variableName}' must refer to a double series in the training inputs.",
                    nameof(trainingData));
            }
        }
    }

    private static ObjectiveDirections CreateObjective(ImmutableArray<IRegressionMetric> predictionMetrics, ImmutableArray<IExpressionMetric> expressionMetrics, IComparer<ObjectiveVector>? totalOrderComparer)
    {
        if (predictionMetrics.Length == 0 && expressionMetrics.Length == 0)
            throw new ArgumentException("At least one prediction metric or expression metric must be supplied.");

        var directions = predictionMetrics
            .Select(metric => metric.Direction)
            .Concat(expressionMetrics.Select(metric => metric.Direction))
            .ToArray();
        var comparer = totalOrderComparer ?? (directions.Length == 1
            ? new SingleObjectiveComparer(directions[0])
            : new LexicographicComparer(directions));

        return new ObjectiveDirections(directions, comparer);
    }
}
