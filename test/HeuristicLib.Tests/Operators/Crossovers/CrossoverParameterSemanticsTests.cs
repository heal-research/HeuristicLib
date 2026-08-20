using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Operators.Crossovers.IntegerVectorCrossovers;
using HEAL.HeuristicLib.Operators.Crossovers.RealVectorCrossovers;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;
using HEAL.HeuristicLib.Tests.TestSupport.Random;

namespace HEAL.HeuristicLib.Tests.Operators.Crossovers;

/// <summary>
/// Crossover settings are retained exactly as configured. Values outside their conventional range are interpreted at
/// run time rather than rejected: a threshold at most zero, or <c>NaN</c>, never applies, and a threshold of at least
/// one always applies. Structural mismatches such as unequal parent lengths remain errors.
/// </summary>
public sealed class CrossoverParameterSemanticsTests
{
    private static readonly IntegerVectorSearchSpace IntegerSearchSpace = new(1, IntegerVector.Create(0), IntegerVector.Create(10));
    private static readonly IRandomNumberGenerator Random = RandomNumberGenerator.Create(42);

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void ThresholdParameters_PreserveUnconventionalValues(double value)
    {
        new RandomCrossover<int> { Bias = value }.Bias.ShouldBe(value);
        new RoundedUniformArithmeticCrossover { Probability = value }.Probability.ShouldBe(value);
    }

    [Theory]
    [InlineData(-0.1, false)]
    [InlineData(double.NegativeInfinity, false)]
    [InlineData(double.NaN, false)]
    [InlineData(1.1, true)]
    [InlineData(double.PositiveInfinity, true)]
    public void Bias_UsesNeverAndAlwaysSemantics(double value, bool takesFirstParent)
    {
        var crossover = new RandomCrossover<int> { Bias = value };

        var result = crossover.CrossParents(Parents.From(1, 2), new SequenceRandomNumberGenerator(0.5));

        result.ShouldBe(takesFirstParent ? 1 : 2);
    }

    [Theory]
    [InlineData(-0.1, false)]
    [InlineData(double.NegativeInfinity, false)]
    [InlineData(double.NaN, false)]
    [InlineData(1.1, true)]
    [InlineData(double.PositiveInfinity, true)]
    public void UniformArithmeticProbability_UsesNeverAndAlwaysSemantics(double value, bool blends)
    {
        var parent1 = IntegerVector.Create(2);
        var parent2 = IntegerVector.Create(8);

        var result = RoundedUniformArithmeticCrossover.Cross(new SequenceRandomNumberGenerator(0.5), parent1, parent2, IntegerSearchSpace, alpha: 0.5, probability: value);

        result.ShouldBe(blends ? IntegerVector.Create(5) : parent1);
    }

    [Theory]
    [InlineData(-0.5)]
    [InlineData(1.5)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void BlendParameters_PreserveUnconventionalValuesAndStayWithinBounds(double value)
    {
        new RoundedBlendAlphaCrossover { Alpha = value }.Alpha.ShouldBe(value);
        new RoundedBlendAlphaBetaCrossover { Alpha = value, Beta = value }.Alpha.ShouldBe(value);
        new RoundedUniformArithmeticCrossover { Alpha = value }.Alpha.ShouldBe(value);

        // A minimum well above zero keeps this honest: a degenerate interval must still be clamped into the bounds
        // rather than collapsing to a default of zero.
        var offsetSearchSpace = new IntegerVectorSearchSpace(1, IntegerVector.Create(3), IntegerVector.Create(10));
        var parent1 = IntegerVector.Create(4);
        var parent2 = IntegerVector.Create(8);

        var blendAlpha = RoundedBlendAlphaCrossover.Cross(Random, [parent1, parent2], offsetSearchSpace, value);
        var blendAlphaBeta = RoundedBlendAlphaBetaCrossover.Cross(Random, parent1, parent2, offsetSearchSpace, value, value);
        var arithmetic = RoundedUniformArithmeticCrossover.Cross(Random, parent1, parent2, offsetSearchSpace, value, probability: 1);

        blendAlpha[0].ShouldBeInRange(3, 10);
        blendAlphaBeta[0].ShouldBeInRange(3, 10);
        arithmetic[0].ShouldBeInRange(3, 10);
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(-0.5)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Contiguity_PreservesUnconventionalValues(double value)
    {
        new SimulatedBinaryCrossover { Contiguity = value }.Contiguity.ShouldBe(value);

        var result = SimulatedBinaryCrossover.Cross(Random, RealVector.Create(1.0), RealVector.Create(2.0), value);

        result.Count.ShouldBe(1);
    }

}
