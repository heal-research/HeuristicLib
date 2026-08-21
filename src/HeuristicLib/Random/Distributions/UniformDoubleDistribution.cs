namespace HEAL.HeuristicLib.Random;

/// <summary>Samples a uniformly distributed value from <c>[minimum, maximum)</c>.</summary>
/// <remarks>
/// Bounds are retained as supplied and use collapsed-range semantics: when the maximum is at most the minimum, the
/// minimum is returned without consuming a random draw. A <see cref="double.NaN"/> bound propagates
/// <see cref="double.NaN"/> and infinite bounds follow IEEE arithmetic.
/// </remarks>
public sealed record UniformDoubleDistribution(double Minimum, double Maximum) : IDistribution<double>
{
    public double Sample(IRandomNumberGenerator random) => Sample(random, Minimum, Maximum);

    public static double Sample(IRandomNumberGenerator random, double minimum, double maximum) =>
        random.NextDouble(minimum, maximum);
}
