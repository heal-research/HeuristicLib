using HEAL.HeuristicLib.DataAnalysis;
using HEAL.HeuristicLib.DataAnalysis.Regression;
using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression;
using HEAL.HeuristicLib.SearchSpaces.SymbolicExpressions;
using static HEAL.HeuristicLib.Genotypes.SymbolicExpressions.ExpressionDraft;

namespace HEAL.HeuristicLib.Tests.Problems.DataAnalysis.Regression;

public sealed class SymbolicRegressionProblemTests
{
    [Fact]
    public void RegressionData_RejectsMismatchedInputAndTargetRows()
    {
        var inputs = DataFrame.FromMatrix(
            ["x0"],
            new double[,]
            {
                { 1.0 },
                { 2.0 }
            });
        var target = new Series<double>("y", [1.0]);

        Should.Throw<ArgumentException>(() => new RegressionData(inputs, target));
    }

    [Fact]
    public void RegressionData_PreservesTargetName()
    {
        var data = CreateLinearRegressionData();

        data.Target.Name.ShouldBe("y");
        data.RowCount.ShouldBe(3);
    }

    [Fact]
    public void Metric_SeriesOverloadMatchesSpanEvaluation()
    {
        var predicted = new Series<double>("prediction", [2.0, 5.0]);
        var target = new Series<double>("target", [1.0, 1.0]);

        Metrics.RMSE.Evaluate(predicted, target)
            .ShouldBe(Metrics.RMSE.Evaluate(predicted.Values.Span, target.Values.Span));
        Metrics.RMSE.Direction.ShouldBe(ObjectiveDirection.Minimize);
        Metrics.R2.Direction.ShouldBe(ObjectiveDirection.Maximize);
    }

    [Fact]
    public void Constructor_RejectsMissingSearchSpaceVariable()
    {
        var data = new RegressionData(
            DataFrame.FromMatrix(["x0"], new double[,] { { 1.0 } }),
            new Series<double>("y", [1.0]));

        Should.Throw<ArgumentException>(() =>
            CreateProblem(data, Metrics.RMSE, variables: ["x0", "missing"]));
    }

    [Fact]
    public void Constructor_RejectsNonDoubleSearchSpaceVariable()
    {
        var data = new RegressionData(
            new DataFrame([new Series<int>("x0", [1])]),
            new Series<double>("y", [1.0]));

        Should.Throw<ArgumentException>(() => CreateProblem(data, Metrics.RMSE));
    }

    [Fact]
    public void Evaluate_UsesBoundTrainingDataAndMetric()
    {
        var problem = CreateProblem(CreateLinearRegressionData(), Metrics.RMSE);

        var objective = problem.Evaluate(CreateLinearExpression());

        problem.PredictionMetrics.ShouldBe([Metrics.RMSE]);
        problem.ExpressionMetrics.ShouldBeEmpty();
        problem.Objective.Directions.ShouldBe([ObjectiveDirection.Minimize]);
        objective.ShouldBe(new ObjectiveVector(0.0));
    }

    [Fact]
    public void Constructor_DefaultsToMeanSquaredError()
    {
        var data = CreateLinearRegressionData();
        var searchSpace = CreateSearchSpace();
        var problem = new SymbolicRegressionProblem(data, searchSpace);

        problem.PredictionMetrics.ShouldBe([Metrics.MSE]);
        problem.ExpressionMetrics.ShouldBeEmpty();
        problem.Evaluate(CreateLinearExpression()).ShouldBe(new ObjectiveVector(0.0));
    }

    [Fact]
    public void Constructor_AcceptsOnePredictionAndOneExpressionMetric()
    {
        var problem = new SymbolicRegressionProblem(
            CreateLinearRegressionData(),
            Metrics.RMSE,
            ExpressionMetrics.Length,
            CreateSearchSpace());

        problem.PredictionMetrics.ShouldBe([Metrics.RMSE]);
        problem.ExpressionMetrics.ShouldBe([ExpressionMetrics.Length]);
    }

    [Fact]
    public void Constructor_AcceptsOnePredictionAndMultipleExpressionMetrics()
    {
        var problem = new SymbolicRegressionProblem(
            CreateLinearRegressionData(),
            Metrics.RMSE,
            [ExpressionMetrics.Length, ExpressionMetrics.VariableCount],
            CreateSearchSpace());

        problem.PredictionMetrics.ShouldBe([Metrics.RMSE]);
        problem.ExpressionMetrics.ShouldBe(
            [ExpressionMetrics.Length, ExpressionMetrics.VariableCount]);
    }

    [Fact]
    public void Evaluate_UsesMetricObjectiveDirection()
    {
        var problem = CreateProblem(CreateLinearRegressionData(), Metrics.R2);

        problem.Evaluate(CreateLinearExpression()).ShouldBe(new ObjectiveVector(1.0));
        problem.Objective.Directions.ShouldBe([ObjectiveDirection.Maximize]);
    }

    [Fact]
    public void Evaluate_OptionallyFitsLinearScalingWithoutChangingExpression()
    {
        var expression = Variable("x0").Build();
        var root = expression.Root;
        var data = new RegressionData(
            DataFrame.FromMatrix(
                ["x0"],
                new double[,]
                {
                    { 1.0 },
                    { 2.0 },
                    { 3.0 }
                }),
            new Series<double>("y", [5.0, 8.0, 11.0]));
        var searchSpace = CreateSearchSpace(["x0"]);
        var unscaled = new SymbolicRegressionProblem(data, Metrics.MSE, searchSpace);
        var scaled = new SymbolicRegressionProblem(data, Metrics.MSE, searchSpace, useLinearScaling: true);

        var unscaledObjective = unscaled.Evaluate(expression);
        var scaledObjective = scaled.Evaluate(expression);

        unscaled.UseLinearScaling.ShouldBeFalse();
        scaled.UseLinearScaling.ShouldBeTrue();
        unscaledObjective[0].ShouldBeGreaterThan(0.0);
        scaledObjective[0].ShouldBe(0.0, tolerance: 1e-12);
        expression.Root.ShouldBeSameAs(root);
        expression.ShouldBe(Variable("x0").Build());
    }

    [Fact]
    public void Evaluate_AppliesOneLinearScalingToAllPredictionMetrics()
    {
        var expression = Variable("x0").Build();
        var data = new RegressionData(
            DataFrame.FromMatrix(
                ["x0"],
                new double[,]
                {
                    { 1.0 },
                    { 2.0 },
                    { 3.0 }
                }),
            new Series<double>("y", [5.0, 8.0, 11.0]));
        var problem = new SymbolicRegressionProblem(
            data,
            [Metrics.MSE, Metrics.MAE],
            [],
            CreateSearchSpace(["x0"]),
            useLinearScaling: true);

        problem.Evaluate(expression).ShouldBe(new ObjectiveVector(0.0, 0.0));
    }

    [Fact]
    public void Evaluate_CombinesPredictionAndExpressionMetricsInDeclaredOrder()
    {
        var data = CreateLinearRegressionData();
        var problem = new SymbolicRegressionProblem(
            data,
            [Metrics.R2, Metrics.MSE],
            [ExpressionMetrics.Length, ExpressionMetrics.VariableCount],
            CreateSearchSpace());

        var objective = problem.Evaluate(CreateLinearExpression());

        objective.ShouldBe(new ObjectiveVector(1.0, 0.0, 5.0, 2.0));
        problem.Objective.Directions.ShouldBe(
            [
                ObjectiveDirection.Maximize,
                ObjectiveDirection.Minimize,
                ObjectiveDirection.Minimize,
                ObjectiveDirection.Minimize
            ]);
        problem.Objective.TotalOrderComparer.ShouldBeOfType<LexicographicComparer>();
    }

    [Fact]
    public void Evaluate_ComputesPredictionsOnceForMultiplePredictionMetrics()
    {
        var symbol = new CountingConstantSymbol();
        var expression = new ExpressionTree(new PayloadlessTerminalExpressionNode(symbol));
        var data = new RegressionData(
            DataFrame.FromMatrix(["x0"], new double[,] { { 1.0 }, { 2.0 } }),
            new Series<double>("y", [1.0, 1.0]));
        var problem = new SymbolicRegressionProblem(
            data,
            [Metrics.MSE, Metrics.RMSE],
            [],
            CreateSearchSpace(["x0"]));

        problem.Evaluate(expression).ShouldBe(new ObjectiveVector(0.0, 0.0));
        symbol.EmitCount.ShouldBe(1);
    }

    [Fact]
    public void Evaluate_ExpressionOnlyObjectiveDoesNotCompileExpression()
    {
        var expression = new ExpressionTree(
            new PayloadlessTerminalExpressionNode(new ThrowingTerminalSymbol()));
        var problem = new SymbolicRegressionProblem(
            CreateLinearRegressionData(),
            [],
            [ExpressionMetrics.Length],
            CreateSearchSpace());

        problem.Evaluate(expression).ShouldBe(new ObjectiveVector(1.0));
    }

    [Fact]
    public void Constructor_RejectsEmptyObjective()
    {
        Should.Throw<ArgumentException>(() =>
            new SymbolicRegressionProblem(
                CreateLinearRegressionData(),
                [],
                [],
                CreateSearchSpace()));
    }

    [Fact]
    public void ValidationPrediction_IsPerformedThroughPredictorAndMetric()
    {
        var predictor = new BoundedRegressor(
            new SymbolicRegressor(CreateLinearExpression(), "prediction"),
            double.NegativeInfinity,
            double.PositiveInfinity);
        var validation = new RegressionData(
            DataFrame.FromMatrix(
                ["x0", "x1"],
                new double[,]
                {
                    { 4.0, 6.0 },
                    { 5.0, 7.0 }
                }),
            new Series<double>("y", [16.0, 19.0]));

        var prediction = predictor.Predict(validation.Inputs);

        Metrics.RMSE.Evaluate(prediction, validation.Target).ShouldBe(0.0);
    }

    private static ExpressionTree CreateLinearExpression() =>
        (Variable("x0") + FixedConstant(2.0) * Variable("x1")).Build();

    private static SymbolicRegressionProblem CreateProblem(
        RegressionData data,
        IRegressionMetric metric,
        IEnumerable<string>? variables = null) =>
        new(
            data,
            metric,
            CreateSearchSpace(variables));

    private static ExpressionTreeSearchSpace CreateSearchSpace(
        IEnumerable<string>? variables = null) =>
        new(
            maximumLength: 31,
            maximumDepth: 6,
            operations: Symbols.MinimalOperations,
            variables: variables ?? ["x0", "x1"],
            constants: [new FixedConstantSymbol(2.0)]);

    private static RegressionData CreateLinearRegressionData() =>
        new(
            DataFrame.FromMatrix(
                ["x0", "x1"],
                new double[,]
                {
                    { 1.0, 3.0 },
                    { 2.0, 4.0 },
                    { 3.0, 5.0 }
                }),
            new Series<double>("y", [7.0, 10.0, 13.0]));

    private sealed record CountingConstantSymbol() : PayloadlessTerminalSymbol("counting-constant")
    {
        public int EmitCount { get; private set; }

        public override void Emit(ExpressionNode node, IExpressionEmitter emitter)
        {
            EmitCount++;
            emitter.EmitConstant(1.0);
        }
    }

    private sealed record ThrowingTerminalSymbol() : PayloadlessTerminalSymbol("throwing")
    {
        public override void Emit(ExpressionNode node, IExpressionEmitter emitter)
        {
            throw new InvalidOperationException("The expression must not be compiled.");
        }
    }
}
