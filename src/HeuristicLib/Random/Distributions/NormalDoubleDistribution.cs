namespace HEAL.HeuristicLib.Random.Distributions;

public sealed record NormalDoubleDistribution(double Mean, double StandardDeviation) : IDistribution<double>
{
    public double Sample(IRandomNumberGenerator random) => Sample(random, Mean, StandardDeviation);

    public static double Sample(IRandomNumberGenerator random, double mean, double standardDeviation)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(standardDeviation);

        return random.NextNormal(mean, standardDeviation);
    }
}
