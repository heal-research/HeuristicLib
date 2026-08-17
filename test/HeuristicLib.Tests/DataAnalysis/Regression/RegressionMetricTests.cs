using HEAL.HeuristicLib.DataAnalysis;
using HEAL.HeuristicLib.DataAnalysis.Regression;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Tests.DataAnalysis.Regression;

public sealed class RegressionMetricTests
{
    [Fact]
    public void MeanSquaredError_EvaluatesValues()
    {
        Metrics.MSE.Evaluate([2.0, 5.0], [1.0, 1.0])
            .ShouldBe(8.5, tolerance: 1e-12);
        Metrics.MSE.Direction.ShouldBe(ObjectiveDirection.Minimize);
    }

    [Fact]
    public void RootMeanSquaredError_EvaluatesValues()
    {
        Metrics.RMSE.Evaluate([2.0, 5.0], [1.0, 1.0])
            .ShouldBe(Math.Sqrt(8.5), tolerance: 1e-12);
        Metrics.RMSE.Direction.ShouldBe(ObjectiveDirection.Minimize);
    }

    [Fact]
    public void RootMeanSquaredError_RejectsMismatchedLengths()
    {
        Should.Throw<ArgumentException>(() => Metrics.RMSE.Evaluate([1.0], [1.0, 2.0]));
    }

    [Fact]
    public void R2Score_EvaluatesValues()
    {
        Metrics.R2.Evaluate([2.5, 0.0, 2.0, 8.0], [3.0, -0.5, 2.0, 7.0])
            .ShouldBe(0.9486081370449679, tolerance: 1e-12);
        Metrics.R2.Direction.ShouldBe(ObjectiveDirection.Maximize);
    }

    [Fact]
    public void R2Score_RejectsConstantTargets()
    {
        Should.Throw<ArgumentException>(() => Metrics.R2.Evaluate([1.0, 2.0], [1.0, 1.0]));
    }

    [Fact]
    public void AbsoluteAndRelativeMetrics_EvaluateValues()
    {
        ReadOnlySpan<double> predictions = [2.0, 2.0, 1.0];
        ReadOnlySpan<double> targets = [1.0, 2.0, 4.0];

        Metrics.MAE.Evaluate(predictions, targets)
            .ShouldBe(4.0 / 3.0, tolerance: 1e-12);
        Metrics.MaxAbsoluteError.Evaluate(predictions, targets)
            .ShouldBe(3.0, tolerance: 1e-12);
        Metrics.MeanLogError.Evaluate(predictions, targets)
            .ShouldBe(Math.Log(2.0), tolerance: 1e-12);
        Metrics.MeanRelativeError.Evaluate(predictions, targets)
            .ShouldBe(11.0 / 30.0, tolerance: 1e-12);
    }

    [Fact]
    public void NormalizedMeanSquaredError_EvaluatesValues()
    {
        Metrics.NMSE.Evaluate([2.0, 2.0, 1.0], [1.0, 2.0, 4.0])
            .ShouldBe(15.0 / 7.0, tolerance: 1e-12);
        Metrics.NMSE.Direction.ShouldBe(ObjectiveDirection.Minimize);
    }

    [Fact]
    public void NormalizedMeanSquaredError_ReturnsZeroForConstantTargets()
    {
        Metrics.NMSE.Evaluate([1.0, 2.0], [1.0, 1.0])
            .ShouldBe(0.0);
    }

    [Fact]
    public void PearsonR2_EvaluatesSquaredCorrelation()
    {
        Metrics.PearsonR2.Evaluate([2.0, 2.0, 1.0], [1.0, 2.0, 4.0])
            .ShouldBe(25.0 / 28.0, tolerance: 1e-12);
        Metrics.PearsonR2.Direction.ShouldBe(ObjectiveDirection.Maximize);
    }

    [Fact]
    public void PearsonR2_ReturnsZeroForConstantSeries()
    {
        Metrics.PearsonR2.Evaluate([1.0, 1.0], [1.0, 2.0])
            .ShouldBe(0.0);
    }

    [Fact]
    public void SeriesOverload_UsesSeriesValues()
    {
        var predictions = new Series<double>("prediction", [2.0, 5.0]);
        var targets = new Series<double>("target", [1.0, 1.0]);

        Metrics.RMSE.Evaluate(predictions, targets)
            .ShouldBe(Math.Sqrt(8.5), tolerance: 1e-12);
    }

    /// <summary>
    /// The replacement is the worst <em>finite</em> value, which is deliberately not
    /// <see cref="ObjectiveValue.WorstValue"/>: that is an infinity, and handing downstream arithmetic a finite number
    /// is the entire purpose of this metric.
    /// </summary>
    [Theory]
    [InlineData(ObjectiveDirection.Minimize, double.MaxValue)]
    [InlineData(ObjectiveDirection.Maximize, double.MinValue)]
    public void ToFinite_ReplacesNonFiniteResultsWithTheWorstFiniteValue(
        ObjectiveDirection direction,
        double expected)
    {
        var metric = new StubRegressionMetric(direction, double.NaN).ToFinite();

        metric.Evaluate([1.0], [1.0]).ShouldBe(expected);
        metric.Direction.ShouldBe(direction);

        double.IsFinite(metric.Evaluate([1.0], [1.0])).ShouldBeTrue();
        double.IsFinite(ObjectiveValue.WorstValue(direction).Value).ShouldBeFalse();
    }

    [Fact]
    public void ToFinite_UsesExplicitReplacementValue()
    {
        var metric = new StubRegressionMetric(ObjectiveDirection.Minimize, double.PositiveInfinity)
            .ToFinite(123.0);

        metric.Evaluate([1.0], [1.0]).ShouldBe(123.0);
        metric.NonFiniteValue.ShouldBe(123.0);
    }

    [Fact]
    public void ToFinite_PreservesFiniteResults()
    {
        var metric = new StubRegressionMetric(ObjectiveDirection.Minimize, 42.0)
            .ToFinite();

        metric.Evaluate([1.0], [1.0]).ShouldBe(42.0);
    }

    [Fact]
    public void ToFinite_RequiresFiniteReplacementValue()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            Metrics.MSE.ToFinite(double.NegativeInfinity));
    }

    private sealed class StubRegressionMetric(ObjectiveDirection direction, double value) : IRegressionMetric
    {
        public ObjectiveDirection Direction => direction;

        public double Evaluate(ReadOnlySpan<double> predictedValues, ReadOnlySpan<double> targetValues) => value;
    }
}
