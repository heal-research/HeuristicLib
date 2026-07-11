using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Regression;

public sealed class SymbolicExpressionRegressionProblem
{
    public SymbolicExpressionRegressionProblem(
        RegressionData data,
        IRegressionMetric metric)
    {
        Data = data;
        Metric = metric;
        Objective = new Objective([metric.Direction], new SingleObjectiveComparer(metric.Direction));
    }

    public RegressionData Data { get; }
    public IRegressionMetric Metric { get; }
    public Objective Objective { get; }

    public double[] Predict(ExpressionTree expression) =>
        expression.Evaluate(Data.TrainingInputs);

    public double[] PredictValidation(ExpressionTree expression) =>
        expression.Evaluate(Data.ValidationInputs ?? throw new InvalidOperationException("Validation data is not available."));

    public double[] PredictTest(ExpressionTree expression) =>
        expression.Evaluate(Data.TestInputs ?? throw new InvalidOperationException("Test data is not available."));

    public ObjectiveVector Evaluate(ExpressionTree expression)
    {
        var predictions = Predict(expression);
        return new ObjectiveVector(Metric.Evaluate(predictions, Data.TrainingTarget.Values.Span));
    }

    public ObjectiveVector EvaluateValidation(ExpressionTree expression)
    {
        var predictions = PredictValidation(expression);
        var target = Data.ValidationTarget ?? throw new InvalidOperationException("Validation data is not available.");
        return new ObjectiveVector(Metric.Evaluate(predictions, target.Values.Span));
    }

    public ObjectiveVector EvaluateTest(ExpressionTree expression)
    {
        var predictions = PredictTest(expression);
        var target = Data.TestTarget ?? throw new InvalidOperationException("Test data is not available.");
        return new ObjectiveVector(Metric.Evaluate(predictions, target.Values.Span));
    }
}
