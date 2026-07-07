using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression;
using static HEAL.HeuristicLib.Genotypes.SymbolicExpressions.ExpressionDraft;

namespace HEAL.HeuristicLib.Tests.Problems.DataAnalysis.Regression;

public sealed class SymbolicExpressionRegressionProblemTests
{
    [Fact]
    public void Training_RejectsMismatchedInputAndTargetRows()
    {
        var inputs = DataFrame.FromMatrix(
          ["x0"],
          new double[,]
          {
              { 1.0 },
              { 2.0 }
          });
        var target = Series<double>.Create([1.0], name: "y");

        Should.Throw<ArgumentException>(() => RegressionData.Training(inputs, target));
    }

    [Fact]
    public void Training_RejectsUnnamedTarget()
    {
        var inputs = DataFrame.FromMatrix(
          ["x0"],
          new double[,]
          {
              { 1.0 }
          });
        var target = Series<double>.Create([1.0]);

        Should.Throw<ArgumentException>(() => RegressionData.Training(inputs, target));
    }

    [Fact]
    public void Training_PreservesTargetName()
    {
        var data = CreateLinearRegressionData();

        data.TargetName.ShouldBe("y");
        data.TrainingTarget.Name.ShouldBe("y");
    }

    [Fact]
    public void WithTrainingAndValidation_RejectsDifferentTargetNames()
    {
        var inputs = DataFrame.FromMatrix(
          ["x0"],
          new double[,]
          {
              { 1.0 }
          });

        Should.Throw<ArgumentException>(() =>
          RegressionData.WithTrainingAndValidation(
            inputs,
            Series<double>.Create([1.0], name: "y"),
            inputs,
            Series<double>.Create([1.0], name: "other")));
    }

    [Fact]
    public void RmseMetric_EvaluatesPredictionAndTargetSeries()
    {
        var value = Metrics.RMSE.Evaluate([2.0, 5.0], [1.0, 1.0]);

        value.ShouldBe(Math.Sqrt(8.5), tolerance: 1e-12);
        Metrics.RMSE.Direction.ShouldBe(ObjectiveDirection.Minimize);
    }

    [Fact]
    public void RmseMetric_RejectsMismatchedSeriesLengths()
    {
        Should.Throw<ArgumentException>(() => Metrics.RMSE.Evaluate([1.0], [1.0, 2.0]));
    }

    [Fact]
    public void R2Metric_EvaluatesPredictionAndTargetSeries()
    {
        var value = Metrics.R2.Evaluate([2.5, 0.0, 2.0, 8.0], [3.0, -0.5, 2.0, 7.0]);

        value.ShouldBe(0.9486081370449679, tolerance: 1e-12);
        Metrics.R2.Direction.ShouldBe(ObjectiveDirection.Maximize);
    }

    [Fact]
    public void R2Metric_RejectsConstantTargetSeries()
    {
        Should.Throw<ArgumentException>(() => Metrics.R2.Evaluate([1.0, 2.0], [1.0, 1.0]));
    }

    [Fact]
    public void Predict_EvaluatesExpressionAgainstTrainingData()
    {
        var problem = new SymbolicExpressionRegressionProblem(
            CreateLinearRegressionData(),
            Metrics.RMSE);
        var expression = CreateLinearExpression();

        var predictions = problem.Predict(expression);

        predictions.ShouldBe([7.0, 10.0, 13.0]);
    }

    [Fact]
    public void Evaluate_UsesMetricAgainstTrainingTargets()
    {
        var problem = new SymbolicExpressionRegressionProblem(
            CreateLinearRegressionData(),
            Metrics.RMSE);
        var expression = CreateLinearExpression();

        var objective = problem.Evaluate(expression);

        problem.Metric.ShouldBe(Metrics.RMSE);
        problem.Objective.Directions.ShouldBe([ObjectiveDirection.Minimize]);
        objective.ShouldBe(new ObjectiveVector(0.0));
    }

    [Fact]
    public void Evaluate_UsesMaximizationDirectionForR2Metric()
    {
        var problem = new SymbolicExpressionRegressionProblem(
            CreateLinearRegressionData(),
            Metrics.R2);
        var expression = CreateLinearExpression();

        var objective = problem.Evaluate(expression);

        problem.Objective.Directions.ShouldBe([ObjectiveDirection.Maximize]);
        objective.ShouldBe(new ObjectiveVector(1.0));
    }

    [Fact]
    public void EvaluateValidation_UsesValidationSplitWhenAvailable()
    {
        var problem = new SymbolicExpressionRegressionProblem(
            CreateLinearRegressionDataWithValidation(),
            Metrics.RMSE);
        var expression = CreateLinearExpression();

        var objective = problem.EvaluateValidation(expression);

        objective.ShouldBe(new ObjectiveVector(0.0));
    }

    [Fact]
    public void EvaluateValidation_RejectsMissingValidationSplit()
    {
        var problem = new SymbolicExpressionRegressionProblem(
            CreateLinearRegressionData(),
            Metrics.RMSE);
        var expression = CreateLinearExpression();

        Should.Throw<InvalidOperationException>(() => problem.EvaluateValidation(expression));
    }

    private static SymbolicExpression CreateLinearExpression() =>
        (Variable("x0") + Fixed(2.0) * Variable("x1")).Build();

    private static RegressionData CreateLinearRegressionData() =>
        RegressionData.Training(
            DataFrame.FromMatrix(
                ["x0", "x1"],
                new double[,]
                {
                    { 1.0, 3.0 },
                    { 2.0, 4.0 },
                    { 3.0, 5.0 }
                }),
            Series<double>.Create([7.0, 10.0, 13.0], name: "y"));

    private static RegressionData CreateLinearRegressionDataWithValidation() =>
        RegressionData.WithTrainingAndValidation(
            DataFrame.FromMatrix(
                ["x0", "x1"],
                new double[,]
                {
                    { 1.0, 3.0 },
                    { 2.0, 4.0 },
                    { 3.0, 5.0 }
                }),
            Series<double>.Create([7.0, 10.0, 13.0], name: "y"),
            DataFrame.FromMatrix(
                ["x0", "x1"],
                new double[,]
                {
                    { 4.0, 6.0 },
                    { 5.0, 7.0 }
                }),
            Series<double>.Create([16.0, 19.0], name: "y"));
}
