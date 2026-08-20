using HEAL.HeuristicLib.Operators.Crossovers.SymbolicExpressionTreeCrossovers;

namespace HEAL.HeuristicLib.Tests.Operators.Crossovers;

public sealed class SymbolicExpressionTreeCrossoverParameterSemanticsTests
{
    [Theory]
    [InlineData(-0.1, false)]
    [InlineData(double.NegativeInfinity, false)]
    [InlineData(double.NaN, false)]
    [InlineData(1.1, true)]
    [InlineData(double.PositiveInfinity, true)]
    public void InternalCrossoverPointProbability_SelectsLeafOrInternalPointsWithoutValidation(double value, bool prefersInternal)
    {
        var crossover = new SubtreeCrossover { InternalCrossoverPointProbability = value };

        crossover.InternalCrossoverPointProbability.ShouldBe(value);
        Should.NotThrow(() => _ = crossover with { InternalCrossoverPointProbability = value });
        (value > 1 || double.IsPositiveInfinity(value)).ShouldBe(prefersInternal);
    }
}
