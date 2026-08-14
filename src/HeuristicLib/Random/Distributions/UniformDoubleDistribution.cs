namespace HEAL.HeuristicLib.Random.Distributions;

public sealed record UniformDoubleDistribution(double Minimum, double Maximum) : IDistribution<double>
{
    public double Sample(IRandomNumberGenerator random) => Sample(random, Minimum, Maximum);

    public static double Sample(IRandomNumberGenerator random, double minimum, double maximum)
    {
        if (!double.IsFinite(minimum))
            throw new ArgumentException("Minimum must be finite.", nameof(minimum));

        if (!double.IsFinite(maximum))
            throw new ArgumentException("Maximum must be finite.", nameof(maximum));

        if (maximum < minimum)
            throw new ArgumentOutOfRangeException(nameof(maximum));

        return random.NextDouble(minimum, maximum);
    }
}
