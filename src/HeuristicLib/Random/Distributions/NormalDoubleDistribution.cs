namespace HEAL.HeuristicLib.Random;

/// <summary>Samples a normally distributed value.</summary>
/// <remarks>
/// Values are retained as supplied. A negative standard deviation mirrors samples around the mean, a
/// <see cref="double.NaN"/> standard deviation or mean propagates <see cref="double.NaN"/>, and infinite values follow
/// IEEE arithmetic.
/// </remarks>
public sealed record NormalDoubleDistribution(double Mean, double StandardDeviation) : IDistribution<double>
{
    public double Sample(IRandomNumberGenerator random) => Sample(random, Mean, StandardDeviation);

    public static double Sample(IRandomNumberGenerator random, double mean, double standardDeviation) =>
        random.NextNormal(mean, standardDeviation);
}
