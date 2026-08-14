using HEAL.HeuristicLib.Algorithms.AutoEC;

namespace HEAL.HeuristicLib.Tests.Algorithms.AutoEC;

public class OnlineWeibullCurveModelTests
{
    [Fact]
    public void Predict_WithoutObservations_ReturnsDefaultValue()
    {
        var model = new OnlineWeibullCurveModel(defaultValue: 42.0);

        model.Predict(100.0).ShouldBe(42.0);
    }

    [Fact]
    public void AddObservation_FitsAsymptoticExponentialCurve()
    {
        var model = new OnlineWeibullCurveModel();
        for (var x = 0; x <= 20; x++)
        {
            model.AddObservation(x, Curve(x));
        }

        model.ObservationCount.ShouldBe(21);
        model.Predict(30).ShouldBe(Curve(30), 0.25);
        model.Parameters.Rate.ShouldBeLessThan(0.0);
    }

    [Fact]
    public void AddObservation_AllowsIncreasingCurves()
    {
        var model = new OnlineWeibullCurveModel();
        for (var x = 0; x <= 20; x++)
        {
            model.AddObservation(x, 100.0 - Curve(x));
        }

        model.Predict(30).ShouldBe(100.0 - Curve(30), 0.25);
        model.Parameters.Scale.ShouldBeLessThan(0.0);
    }

    private static double Curve(double x) => 10.0 + 90.0 * Math.Exp(-0.12 * x);
}
