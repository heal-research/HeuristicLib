using HEAL.HeuristicLib.DataAnalysis;
using HEAL.HeuristicLib.DataAnalysis.Regression;

namespace HEAL.HeuristicLib.Tests.DataAnalysis.Regression;

public sealed class LinearScalingTests
{
    [Fact]
    public void Fit_CalculatesLeastSquaresSlopeAndIntercept()
    {
        var parameters = LinearScaling.Fit([1.0, 2.0, 3.0], [5.0, 8.0, 11.0]);

        parameters.Slope.ShouldBe(3.0, tolerance: 1e-12);
        parameters.Intercept.ShouldBe(2.0, tolerance: 1e-12);
    }

    [Fact]
    public void Fit_MapsConstantPredictionsToTargetMean()
    {
        var parameters = LinearScaling.Fit([4.0, 4.0, 4.0], [1.0, 2.0, 6.0]);
        Span<double> predictions = stackalloc double[3];

        LinearScaling.Apply([4.0, 4.0, 4.0], parameters, predictions);

        parameters.Slope.ShouldBe(0.0);
        parameters.Intercept.ShouldBe(3.0, tolerance: 1e-12);
        predictions.ToArray().ShouldAllBe(value => Math.Abs(value - 3.0) < 1e-12);
    }

    [Fact]
    public void Fit_RemainsStableForPredictionsWithALargeOffset()
    {
        double[] predictions = [1e12 + 1.0, 1e12 + 2.0, 1e12 + 3.0];

        var parameters = LinearScaling.Fit(predictions, [1.0, 2.0, 3.0]);

        parameters.Slope.ShouldBe(1.0, tolerance: 1e-12);
        parameters.Intercept.ShouldBe(-1e12, tolerance: 1e-3);
    }

    [Fact]
    public void Apply_SupportsInPlaceTransformation()
    {
        var values = new[] { -1.0, 0.0, 2.0 };

        LinearScaling.Apply(values, new LinearScalingParameters(2.0, 3.0), values);

        values.ShouldBe([1.0, 3.0, 7.0]);
    }

    [Fact]
    public void Apply_RejectsPartialOverlapWithoutModifyingValues()
    {
        var values = new[] { 1.0, 2.0, 3.0, 4.0 };

        Should.Throw<ArgumentException>(() =>
            LinearScaling.Apply(
                values.AsSpan(0, 3),
                new LinearScalingParameters(2.0, 1.0),
                values.AsSpan(1, 3)));

        values.ShouldBe([1.0, 2.0, 3.0, 4.0]);
    }

    [Fact]
    public void Fit_RejectsMissingOrMismatchedObservations()
    {
        Should.Throw<ArgumentException>(() => LinearScaling.Fit([], []));
        Should.Throw<ArgumentException>(() => LinearScaling.Fit([1.0], [1.0, 2.0]));
    }

    [Fact]
    public void Fit_CreatesRegressorUsingTrainingScaleForFuturePredictions()
    {
        var regressor = new InputRegressor("x", "estimate");
        var trainingData = new RegressionData(
            DataFrame.FromMatrix(
                ["x"],
                new double[,]
                {
                    { 1.0 },
                    { 2.0 },
                    { 3.0 }
                }),
            new Series<double>("y", [5.0, 8.0, 11.0]));
        var inputs = DataFrame.FromMatrix(
            ["x"],
            new double[,]
            {
                { 4.0 },
                { 5.0 }
            });

        var scaled = regressor.FitLinearScaling(trainingData);
        var prediction = scaled.Predict(inputs);

        scaled.Regressor.ShouldBeSameAs(regressor);
        scaled.Parameters.Slope.ShouldBe(3.0, tolerance: 1e-12);
        scaled.Parameters.Intercept.ShouldBe(2.0, tolerance: 1e-12);
        scaled.PredictionName.ShouldBe("estimate");
        prediction.Name.ShouldBe("estimate");
        prediction.Values.ToArray().ShouldBe([14.0, 17.0], tolerance: 1e-12);
    }

    [Fact]
    public void Predict_RejectsMismatchedDestinationLength()
    {
        var regressor = new LinearlyScaledRegressor(
            new InputRegressor("x", "prediction"),
            new LinearScalingParameters(2.0, 1.0));
        var inputs = DataFrame.FromMatrix(["x"], new double[,] { { 1.0 } });

        Should.Throw<ArgumentException>(() => regressor.Predict(inputs, new double[2]));
    }

    private sealed class InputRegressor(string variableName, string predictionName) : IRegressor
    {
        public string PredictionName { get; } = predictionName;

        public Series<double> Predict(DataFrame inputs)
        {
            var values = inputs.Get<double>(variableName).Values.ToArray();
            return Series<double>.FromOwnedArray(PredictionName, values);
        }

        public void Predict(DataFrame inputs, Span<double> destination)
        {
            if (destination.Length != inputs.RowCount)
                throw new ArgumentException("Destination length must match the input row count.", nameof(destination));

            inputs.Get<double>(variableName).Values.Span.CopyTo(destination);
        }
    }
}
