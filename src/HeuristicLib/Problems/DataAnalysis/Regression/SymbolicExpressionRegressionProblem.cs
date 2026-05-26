using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Regression;

public sealed class SymbolicExpressionRegressionProblem
{
    public SymbolicExpressionRegressionProblem(
      RegressionData data,
      RegressionEvaluator metric)
    {
        Data = data;
        Metric = metric;
        Objective = new Objective([metric.Direction], new SingleObjectiveComparer(metric.Direction));
    }

    public RegressionData Data { get; }
    public RegressionEvaluator Metric { get; }
    public Objective Objective { get; }

    public double[] Predict(SymbolicExpression expression) =>
      expression.Evaluate(Data.TrainingInputs);

    public double[] PredictValidation(SymbolicExpression expression) =>
      expression.Evaluate(Data.ValidationInputs ?? throw new InvalidOperationException("Validation data is not available."));

    public double[] PredictTest(SymbolicExpression expression) =>
      expression.Evaluate(Data.TestInputs ?? throw new InvalidOperationException("Test data is not available."));

    public ObjectiveVector Evaluate(SymbolicExpression expression)
    {
        var predictions = Predict(expression);
        return new ObjectiveVector(Metric.Evaluate(predictions, Data.TrainingTarget.Values.ToArray()));
    }

    public ObjectiveVector EvaluateValidation(SymbolicExpression expression)
    {
        var predictions = PredictValidation(expression);
        var target = Data.ValidationTarget ?? throw new InvalidOperationException("Validation data is not available.");
        return new ObjectiveVector(Metric.Evaluate(predictions, target.Values.ToArray()));
    }

    public ObjectiveVector EvaluateTest(SymbolicExpression expression)
    {
        var predictions = PredictTest(expression);
        var target = Data.TestTarget ?? throw new InvalidOperationException("Test data is not available.");
        return new ObjectiveVector(Metric.Evaluate(predictions, target.Values.ToArray()));
    }
}
