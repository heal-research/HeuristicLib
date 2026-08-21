using HEAL.HeuristicLib.Numerics;

namespace HEAL.HeuristicLib.Algorithms.AutoEC;

/// <summary>
/// Incrementally fits an asymptotic convergence curve of the form y(x) = a + b * exp(c * x).
/// </summary>
public sealed class OnlineWeibullCurveModel
{
    private const double MaximumPrediction = int.MaxValue / 4.0;

    private readonly List<(double X, double Y)> observations = [];
    private readonly double[,] cMatrix = new double[2, 2];
    private readonly double[] cVector = new double[2];
    private double integratedArea;

    public OnlineWeibullCurveModel(double defaultValue = 0.0)
    {
        DefaultValue = defaultValue;
        Parameters = new WeibullCurveParameters(defaultValue, 0.0, 0.0);
    }

    public double DefaultValue { get; }
    public int ObservationCount => observations.Count;
    public WeibullCurveParameters Parameters { get; private set; }

    public void AddObservation(double x, double y)
    {
        if (!double.IsFinite(x))
        {
            throw new ArgumentOutOfRangeException(nameof(x), x, "Observation x value must be finite.");
        }

        if (!double.IsFinite(y))
        {
            throw new ArgumentOutOfRangeException(nameof(y), y, "Observation y value must be finite.");
        }

        observations.Add((x, y));
        if (observations.Count == 1)
        {
            Parameters = new WeibullCurveParameters(y, 0.0, 0.0);
            return;
        }

        var previous = observations[^2];
        integratedArea += 0.5 * (y + previous.Y) * (x - previous.X);

        var first = observations[0];
        var rate = UpdateRate(x - first.X, y - first.Y, integratedArea);
        if (!double.IsFinite(rate))
        {
            Parameters = new WeibullCurveParameters(y, 0.0, 0.0);
            return;
        }

        var exponentials = observations.Select(point => SafeExp(point.X * rate)).ToArray();
        var exponentialSum = exponentials.Sum();
        var normalMatrix = new[,]
        {
            { observations.Count, exponentialSum },
            { exponentialSum, exponentials.Sum(value => value * value) }
        };
        var normalVector = new[]
        {
            observations.Sum(point => point.Y),
            observations.Zip(exponentials, (point, exponential) => point.Y * exponential).Sum()
        };

        var coefficients = InvertAndMultiply(normalMatrix, normalVector);
        if (coefficients is null || coefficients.Any(value => !double.IsFinite(value)))
        {
            Parameters = new WeibullCurveParameters(y, 0.0, 0.0);
            return;
        }

        Parameters = new WeibullCurveParameters(coefficients[0], coefficients[1], rate);
    }

    public void AddObservations(IEnumerable<(double X, double Y)> values)
    {
        foreach (var (x, y) in values)
        {
            AddObservation(x, y);
        }
    }

    public double Predict(double x)
    {
        if (observations.Count == 0)
        {
            return DefaultValue;
        }

        var value = Parameters.Asymptote + Parameters.Scale * SafeExp(x * Parameters.Rate);
        if (!double.IsFinite(value))
        {
            return value < 0 ? -MaximumPrediction : MaximumPrediction;
        }

        return Math.Clamp(value, -MaximumPrediction, MaximumPrediction);
    }

    private double UpdateRate(double xDelta, double yDelta, double area)
    {
        cMatrix[0, 0] += xDelta * xDelta;
        cMatrix[0, 1] += xDelta * area;
        cMatrix[1, 1] += area * area;
        cVector[0] += yDelta * xDelta;
        cVector[1] += yDelta * area;

        var determinant = cMatrix[0, 0] * cMatrix[1, 1] - cMatrix[0, 1] * cMatrix[0, 1];
        if (determinant.IsAlmost(0.0, 1e-15))
        {
            return double.NaN;
        }

        return (-cVector[0] * cMatrix[0, 1] + cVector[1] * cMatrix[0, 0]) / determinant;
    }

    private static double[]? InvertAndMultiply(double[,] matrix, IReadOnlyList<double> vector)
    {
        var determinant = matrix[0, 0] * matrix[1, 1] - matrix[0, 1] * matrix[0, 1];
        if (determinant.IsAlmost(0.0, 1e-15))
        {
            return null;
        }

        var r1 = vector[0] * matrix[1, 1] - vector[1] * matrix[0, 1];
        var r2 = -vector[0] * matrix[0, 1] + vector[1] * matrix[0, 0];
        return [r1 / determinant, r2 / determinant];
    }

    private static double SafeExp(double value)
    {
        if (value > 709.0)
        {
            return double.PositiveInfinity;
        }

        if (value < -745.0)
        {
            return 0.0;
        }

        return Math.Exp(value);
    }
}

public readonly record struct WeibullCurveParameters(double Asymptote, double Scale, double Rate);
