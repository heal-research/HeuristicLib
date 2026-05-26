using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression.Evaluators;

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
    public void Predict_EvaluatesExpressionAgainstTrainingData()
    {
        var problem = new SymbolicExpressionRegressionProblem(
          CreateLinearRegressionData(),
          new RootMeanSquaredErrorEvaluator());
        var expression = CreateLinearExpression();

        var predictions = problem.Predict(expression);

        predictions.ShouldBe([7.0, 10.0, 13.0]);
    }

    [Fact]
    public void Evaluate_UsesMetricAgainstTrainingTargets()
    {
        var problem = new SymbolicExpressionRegressionProblem(
          CreateLinearRegressionData(),
          new RootMeanSquaredErrorEvaluator());
        var expression = CreateLinearExpression();

        var objective = problem.Evaluate(expression);

        problem.Objective.Directions.ShouldBe([ObjectiveDirection.Minimize]);
        objective.ShouldBe(new ObjectiveVector(0.0));
    }

    [Fact]
    public void EvaluateValidation_UsesValidationSplitWhenAvailable()
    {
        var problem = new SymbolicExpressionRegressionProblem(
          CreateLinearRegressionDataWithValidation(),
          new RootMeanSquaredErrorEvaluator());
        var expression = CreateLinearExpression();

        var objective = problem.EvaluateValidation(expression);

        objective.ShouldBe(new ObjectiveVector(0.0));
    }

    [Fact]
    public void EvaluateValidation_RejectsMissingValidationSplit()
    {
        var problem = new SymbolicExpressionRegressionProblem(
          CreateLinearRegressionData(),
          new RootMeanSquaredErrorEvaluator());
        var expression = CreateLinearExpression();

        Should.Throw<InvalidOperationException>(() => problem.EvaluateValidation(expression));
    }

    private static SymbolicExpression CreateLinearExpression() =>
      ExpressionDraft
        .Add(
          ExpressionDraft.Variable("x0"),
          ExpressionDraft.Multiply(
            ExpressionDraft.Fixed(2.0),
            ExpressionDraft.Variable("x1")))
        .Compile();

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
