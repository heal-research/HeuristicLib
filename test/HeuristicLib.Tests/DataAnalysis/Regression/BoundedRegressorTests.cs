using HEAL.HeuristicLib.DataAnalysis;
using HEAL.HeuristicLib.DataAnalysis.Regression;

namespace HEAL.HeuristicLib.Tests.DataAnalysis.Regression;

public sealed class BoundedRegressorTests
{
    [Fact]
    public void ToBounded_CreatesDecoratorForRegressor()
    {
        var inner = new InputRegressor("x0", "prediction");

        var bounded = inner.ToBounded(-1.0, 1.0);

        bounded.Regressor.ShouldBeSameAs(inner);
        bounded.LowerBound.ShouldBe(-1.0);
        bounded.UpperBound.ShouldBe(1.0);
    }

    [Fact]
    public void Predict_ClampsWrappedRegressorAndPreservesPredictionName()
    {
        var inner = new InputRegressor("x0", "prediction");
        var regressor = new BoundedRegressor(inner, lowerBound: 0.0, upperBound: 1.0);
        var inputs = DataFrame.FromMatrix(
            ["x0"],
            new double[,]
            {
                { -2.0 },
                { 0.5 },
                { 3.0 }
            });

        var prediction = regressor.Predict(inputs);

        regressor.Regressor.ShouldBeSameAs(inner);
        regressor.PredictionName.ShouldBe("prediction");
        prediction.Name.ShouldBe("prediction");
        prediction.Values.ToArray().ShouldBe([0.0, 0.5, 1.0]);
    }

    [Fact]
    public void Predict_WritesClampedValuesToTheSuppliedDestination()
    {
        var regressor = new BoundedRegressor(
            new InputRegressor("x0", "prediction"),
            lowerBound: -1.0,
            upperBound: 1.0);
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

        destination.ToArray().ShouldBe([-1.0, 0.0, 1.0]);
    }

    [Fact]
    public void Predict_RejectsMismatchedDestinationLength()
    {
        var regressor = new BoundedRegressor(
            new InputRegressor("x0", "prediction"),
            double.NegativeInfinity,
            double.PositiveInfinity);
        var inputs = DataFrame.FromMatrix(["x0"], new double[,] { { 1.0 } });

        Should.Throw<ArgumentException>(() => regressor.Predict(inputs, new double[2]));
    }

    [Fact]
    public void Constructor_PreservesRegressorAndBounds()
    {
        var inner = new InputRegressor("x0", "prediction");

        var regressor = new BoundedRegressor(
            inner,
            double.NegativeInfinity,
            double.PositiveInfinity);

        regressor.Regressor.ShouldBeSameAs(inner);
        regressor.LowerBound.ShouldBe(double.NegativeInfinity);
        regressor.UpperBound.ShouldBe(double.PositiveInfinity);
    }

    [Theory]
    [InlineData(double.NaN, 1.0)]
    [InlineData(0.0, double.NaN)]
    [InlineData(2.0, 1.0)]
    public void Constructor_RejectsInvalidBounds(double lowerBound, double upperBound)
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            new BoundedRegressor(
                new InputRegressor("x0", "prediction"),
                lowerBound,
                upperBound));
    }

    private sealed class InputRegressor(string variableName, string predictionName) : IRegressor
    {
        public string PredictionName { get; } = predictionName;

        public Series<double> Predict(DataFrame inputs)
        {
            return new Series<double>(PredictionName, inputs.Get<double>(variableName).Values.ToArray());
        }

        public void Predict(DataFrame inputs, Span<double> destination)
        {
            if (destination.Length != inputs.RowCount)
                throw new ArgumentException("Destination length must match the input row count.", nameof(destination));

            inputs.Get<double>(variableName).Values.Span.CopyTo(destination);
        }
    }
}
