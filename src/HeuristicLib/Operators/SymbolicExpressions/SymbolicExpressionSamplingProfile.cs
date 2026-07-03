using HEAL.HeuristicLib.Random.Distributions;

namespace HEAL.HeuristicLib.Operators.SymbolicExpressions;

public sealed record SymbolicExpressionSamplingProfile
{
    public static SymbolicExpressionSamplingProfile Default { get; } = new();

    public IDistribution<double> NumericLiteralInitializationDistribution { get; init; } = new UniformDoubleDistribution(-1.0, 1.0);
}
