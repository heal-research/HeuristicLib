namespace HEAL.HeuristicLib.Random.Distributions;

public sealed record NormalDoubleDistribution(double Mean, double StandardDeviation) : IDistribution<double>
{
    public double Sample(IRandomNumberGenerator random) => Sample(random, Mean, StandardDeviation);

    public static double Sample(IRandomNumberGenerator random, double mean, double standardDeviation)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(standardDeviation);

        double u;
        double s;
        do
        {
            u = (random.NextDouble() * 2) - 1;
            var v = (random.NextDouble() * 2) - 1;
            s = (u * u) + (v * v);
        } while (s is > 1 or 0);

        s = Math.Sqrt(-2.0 * Math.Log(s) / s);
        return mean + (standardDeviation * u * s);
    }
}
