using static HEAL.HeuristicLib.Encodings.SymbolicExpressions.ExpressionDraft;

namespace HEAL.HeuristicLib.Tests.MachineLearning;

public sealed class SymbolicRegressorTests
{
    [Fact]
    public void ToRegressor_CreatesNamedRegressorForExpression()
    {
        var expression = Variable("x0").Build();

        var defaultRegressor = expression.ToRegressor();
        var namedRegressor = expression.ToRegressor("estimate");

        defaultRegressor.Expression.ShouldBeSameAs(expression);
        defaultRegressor.PredictionName.ShouldBe("prediction");
        namedRegressor.Expression.ShouldBeSameAs(expression);
        namedRegressor.PredictionName.ShouldBe("estimate");
    }

    [Fact]
    public void Predict_EvaluatesExpressionAndNamesThePrediction()
    {
        var expression = (Variable("x0") + FixedConstant(1.0)).Build();
        var regressor = new SymbolicRegressor(expression, "estimate");
        var inputs = DataFrame.FromMatrix(
            ["x0"],
            new double[,]
            {
                { 1.0 },
                { 2.0 }
            });

        var prediction = regressor.Predict(inputs);

        regressor.Expression.ShouldBeSameAs(expression);
        regressor.PredictionName.ShouldBe("estimate");
        prediction.Name.ShouldBe("estimate");
        prediction.Values.ToArray().ShouldBe([2.0, 3.0]);
    }

    [Fact]
    public void Predict_ResolvesCachedVariableReferencesByName()
    {
        var regressor = new SymbolicRegressor(
            (Variable("x0") + FixedConstant(2.0) * Variable("x1")).Build());
        var x0ThenX1 = DataFrame.FromMatrix(
            ["x0", "x1"],
            new double[,]
            {
                { 1.0, 3.0 },
                { 2.0, 4.0 }
            });
        var x1ThenX0 = DataFrame.FromMatrix(
            ["x1", "x0"],
            new double[,]
            {
                { 3.0, 1.0 },
                { 4.0, 2.0 }
            });

        regressor.Predict(x0ThenX1).Values.ToArray().ShouldBe([7.0, 10.0]);
        regressor.Predict(x1ThenX0).Values.ToArray().ShouldBe([7.0, 10.0]);
    }

    [Fact]
    public void Predict_WritesToTheSuppliedDestination()
    {
        var regressor = new SymbolicRegressor(Variable("x0").Build());
        var inputs = DataFrame.FromMatrix(
            ["x0"],
            new double[,]
            {
                { -2.0 },
                { 0.0 },
                { 2.0 }
            });
        Span<double> destination = stackalloc double[3];

        regressor.Predict(inputs, destination);

        destination.ToArray().ShouldBe([-2.0, 0.0, 2.0]);
    }

    [Fact]
    public void Predict_RejectsMismatchedDestinationLength()
    {
        var regressor = new SymbolicRegressor(Variable("x0").Build());
        var inputs = DataFrame.FromMatrix(["x0"], new double[,] { { 1.0 } });

        Should.Throw<ArgumentException>(() => regressor.Predict(inputs, new double[2]));
    }

    [Fact]
    public void Constructor_CompilesExpressionOnlyOnce()
    {
        var symbol = new CountingConstantSymbol();
        var expression = new ExpressionTree(new PayloadlessTerminalExpressionNode(symbol));
        var regressor = new SymbolicRegressor(expression);
        var inputs = DataFrame.FromMatrix(["x"], new double[,] { { 0.0 }, { 0.0 } });

        regressor.Predict(inputs);
        regressor.Predict(inputs);

        symbol.EmitCount.ShouldBe(1);
        regressor.CompiledExpression.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_RejectsMissingPredictionName()
    {
        Should.Throw<ArgumentException>(() =>
            new SymbolicRegressor(Variable("x0").Build(), ""));
    }

    private sealed record CountingConstantSymbol() : PayloadlessTerminalSymbol("counting-constant")
    {
        public int EmitCount { get; private set; }

        public override void Emit(ExpressionNode node, IExpressionEmitter emitter)
        {
            EmitCount++;
            emitter.EmitConstant(1.0);
        }
    }
}
