using System.Buffers;
using HEAL.HeuristicLib.Encodings.SymbolicExpressions;
using HEAL.HeuristicLib.MachineLearning;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Problems.MachineLearning;

public sealed class SymbolicRegressionProblem
    : SingleSolutionProblem<SymbolicRegressionProblem, ExpressionTree, ExpressionTreeSearchSpace>
{
    public SymbolicRegressionProblem(RegressionData trainingData, ExpressionTreeSearchSpace searchSpace, bool useLinearScaling = false)
        : this(trainingData, [Metrics.MSE], [], searchSpace, useLinearScaling)
    {
    }

    public SymbolicRegressionProblem(RegressionData trainingData, IRegressionMetric predictionMetric, ExpressionTreeSearchSpace searchSpace, bool useLinearScaling = false)
        : this(trainingData, [predictionMetric], [], searchSpace, useLinearScaling)
    {
    }

    public SymbolicRegressionProblem(RegressionData trainingData, IRegressionMetric predictionMetric, IExpressionMetric expressionMetric, ExpressionTreeSearchSpace searchSpace, bool useLinearScaling = false)
        : this(trainingData, [predictionMetric], [expressionMetric], searchSpace, useLinearScaling)
    {
    }

    public SymbolicRegressionProblem(RegressionData trainingData, IRegressionMetric predictionMetric, IEnumerable<IExpressionMetric> expressionMetrics, ExpressionTreeSearchSpace searchSpace, bool useLinearScaling = false)
        : this(trainingData, [predictionMetric], expressionMetrics.ToImmutableArray(), searchSpace, useLinearScaling)
    {
    }

    public SymbolicRegressionProblem(RegressionData trainingData, IEnumerable<IRegressionMetric> predictionMetrics, IEnumerable<IExpressionMetric> expressionMetrics, ExpressionTreeSearchSpace searchSpace, bool useLinearScaling = false, IComparer<ObjectiveVector>? totalOrderComparer = null)
        : this(trainingData, predictionMetrics.ToImmutableArray(), expressionMetrics.ToImmutableArray(), searchSpace, useLinearScaling, totalOrderComparer)
    {
    }

    private SymbolicRegressionProblem(RegressionData trainingData, ImmutableArray<IRegressionMetric> predictionMetrics, ImmutableArray<IExpressionMetric> expressionMetrics, ExpressionTreeSearchSpace searchSpace, bool useLinearScaling, IComparer<ObjectiveVector>? totalOrderComparer)
        : base(CreateObjective(predictionMetrics, expressionMetrics, totalOrderComparer), searchSpace)
    {
        ValidateVariables(trainingData, searchSpace);
        TrainingData = trainingData;
        PredictionMetrics = predictionMetrics;
        ExpressionMetrics = expressionMetrics;
        UseLinearScaling = useLinearScaling;
    }

    public RegressionData TrainingData { get; }
    public ImmutableArray<IRegressionMetric> PredictionMetrics { get; }
    public ImmutableArray<IExpressionMetric> ExpressionMetrics { get; }
    public bool UseLinearScaling { get; }

    public ObjectiveVector Evaluate(ExpressionTree expression)
    {
        var values = new double[PredictionMetrics.Length + ExpressionMetrics.Length];

        if (PredictionMetrics.Length > 0)
        {
            var rowCount = TrainingData.Inputs.RowCount;
            var buffer = ArrayPool<double>.Shared.Rent(rowCount);
            try
            {
                var predictions = buffer.AsSpan(0, rowCount);
                expression.Evaluate(TrainingData.Inputs, predictions);
                var targets = TrainingData.Target.Values.Span;
                if (UseLinearScaling)
                {
                    var parameters = LinearScaling.Fit(predictions, targets);
                    LinearScaling.Apply(predictions, parameters, predictions);
                }

                for (var i = 0; i < PredictionMetrics.Length; i++)
                    values[i] = ((IPredictionMetric<double>)PredictionMetrics[i]).Evaluate(predictions, targets);
            }
            finally
            {
                ArrayPool<double>.Shared.Return(buffer);
            }
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
                throw new ArgumentException($"The search-space variable '{variableName}' must refer to a double series in the training inputs.", nameof(trainingData));
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
