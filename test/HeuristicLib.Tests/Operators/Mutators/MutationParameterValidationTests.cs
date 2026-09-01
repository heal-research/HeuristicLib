using HEAL.HeuristicLib.Encodings.IntegerVectors;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Tests.TestSupport.Random;

namespace HEAL.HeuristicLib.Tests.Operators.Mutators;

public sealed class MutatorParameterSemanticsTests
{
    private static readonly IntegerVector emptyIntegerVector = IntegerVector.Create();
    private static readonly RealVector emptyRealVector = RealVector.Create();
    private static readonly IntegerVectorSearchSpace emptyIntegerSearchSpace = new(0, IntegerVector.Create(0), IntegerVector.Create(1));
    private static readonly BoundedRealVectorSearchSpace emptyRealSearchSpace = new(0, minimum: 0, maximum: 1);
    private static readonly IRandomNumberGenerator random = RandomNumberGenerator.Create(42);

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void ThresholdParameters_PreserveUnconventionalValues(double value)
    {
        var gaussian = new GaussianMutator(value, mutationStrength: 1);
        var uniform = new UniformSomePositionsMutator { Probability = value };

        gaussian.MutationRate.ShouldBe(value);
        uniform.Probability.ShouldBe(value);
        GaussianMutator.Mutate(emptyRealVector, random, emptyRealSearchSpace, value, mutationStrength: 1).ShouldBeSameAs(emptyRealVector);
        UniformSomePositionsMutator.Mutate(emptyIntegerVector, random, emptyIntegerSearchSpace, value).ShouldBeSameAs(emptyIntegerVector);
    }

    [Theory]
    [InlineData(-0.1, false)]
    [InlineData(double.NegativeInfinity, false)]
    [InlineData(double.NaN, false)]
    [InlineData(1.1, true)]
    [InlineData(double.PositiveInfinity, true)]
    public void ThresholdParameters_UseNeverAndAlwaysSemantics(double value, bool mutates)
    {
        var candidate = IntegerVector.Create(0);
        var searchSpace = new IntegerVectorSearchSpace(1, IntegerVector.Create(1), IntegerVector.Create(2));
        var result = UniformSomePositionsMutator.Mutate(candidate, new SequenceRandomNumberGenerator(0.5, 0.5), searchSpace, value, atLeastOnce: false);

        result.ShouldBe(mutates ? IntegerVector.Create(2) : candidate);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void ArithmeticMutationSettings_PreserveUnconventionalValues(double value)
    {
        var gaussian = new GaussianMutator(mutationRate: 1, mutationStrength: value);
        var polynomial = new PolynomialMutator { Eta = value };
        var instance = new ExecutionInstanceRegistry().Resolve<IVariableStrengthMutatorInstance<RealVector, BoundedRealVectorSearchSpace, IProblem<RealVector, BoundedRealVectorSearchSpace>>>(gaussian);

        instance.CurrentMutationStrength = value;

        gaussian.MutationStrength.ShouldBe(value);
        polynomial.Eta.ShouldBe(value);
        instance.CurrentMutationStrength.ShouldBe(value);
        GaussianMutator.Mutate(emptyRealVector, random, emptyRealSearchSpace, mutationRate: 1, mutationStrength: value).ShouldBeSameAs(emptyRealVector);
        PolynomialMutator.Mutate(emptyRealVector, random, emptyRealSearchSpace, eta: value, atLeastOnce: false).ShouldBeSameAs(emptyRealVector);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void NonFiniteSigma_ProducesBoundedIntegerCandidates(double value)
    {
        var sigma = RealVector.Create(value);
        var candidate = IntegerVector.Create(5);
        var searchSpace = new IntegerVectorSearchSpace(1, IntegerVector.Create(0), IntegerVector.Create(10));
        var expected = double.IsPositiveInfinity(value) ? 10 : 0;

        var onePosition = RoundedNormalOnePositionMutator.Mutate(candidate, new SequenceRandomNumberGenerator(0, 0.75, 0.5), searchSpace, sigma);
        var allPositions = RoundedNormalAllPositionsMutator.Mutate(candidate, new SequenceRandomNumberGenerator(0.75, 0.5), searchSpace, sigma);

        onePosition.ShouldBe([expected]);
        allPositions.ShouldBe([expected]);
    }

    [Fact]
    public void NegativeSigma_IsPreserved()
    {
        var sigma = RealVector.Create(-0.1);
        var onePosition = new RoundedNormalOnePositionMutator { Sigma = sigma };
        var allPositions = new RoundedNormalAllPositionsMutator { Sigma = sigma };

        onePosition.Sigma.ShouldBeSameAs(sigma);
        allPositions.Sigma.ShouldBeSameAs(sigma);
        RoundedNormalOnePositionMutator.Mutate(emptyIntegerVector, random, emptyIntegerSearchSpace, sigma).ShouldBeSameAs(emptyIntegerVector);
        RoundedNormalAllPositionsMutator.Mutate(emptyIntegerVector, random, emptyIntegerSearchSpace, sigma).ShouldBeSameAs(emptyIntegerVector);
    }

    [Fact]
    public void EmptySigma_IsCompatibleWithAnEmptyCandidate()
    {
        var sigma = RealVector.Create();
        var onePosition = new RoundedNormalOnePositionMutator { Sigma = sigma };
        var allPositions = new RoundedNormalAllPositionsMutator { Sigma = sigma };

        onePosition.Sigma.ShouldBeSameAs(sigma);
        allPositions.Sigma.ShouldBeSameAs(sigma);
        RoundedNormalOnePositionMutator.Mutate(emptyIntegerVector, random, emptyIntegerSearchSpace, sigma).ShouldBeSameAs(emptyIntegerVector);
        RoundedNormalAllPositionsMutator.Mutate(emptyIntegerVector, random, emptyIntegerSearchSpace, sigma).ShouldBeSameAs(emptyIntegerVector);
    }

    [Fact]
    public void Sigma_MustBeScalarOrMatchTheCandidateLength()
    {
        var candidate = IntegerVector.Create(1, 2, 3);
        var sigma = RealVector.Create(1, 2);
        var searchSpace = new IntegerVectorSearchSpace(3, IntegerVector.Create(0), IntegerVector.Create(10));
        var randomWithoutValues = new SequenceRandomNumberGenerator();

        Should.Throw<ArgumentException>(() => RoundedNormalOnePositionMutator.Mutate(candidate, randomWithoutValues, searchSpace, sigma))
            .Message.ShouldContain("Minimum, maximum, and sigma");
        Should.Throw<ArgumentException>(() => RoundedNormalAllPositionsMutator.Mutate(candidate, randomWithoutValues, searchSpace, sigma))
            .Message.ShouldContain("Minimum, maximum, and sigma");
    }

    [Fact]
    public void CandidateLengthSigma_IsAccepted()
    {
        var candidate = IntegerVector.Create(5, 5);
        var sigma = RealVector.Create(0, 0);
        var searchSpace = new IntegerVectorSearchSpace(2, IntegerVector.Create(0), IntegerVector.Create(10));

        RoundedNormalOnePositionMutator.Mutate(candidate, RandomNumberGenerator.Create(42), searchSpace, sigma).ShouldBe(candidate);
        RoundedNormalAllPositionsMutator.Mutate(candidate, RandomNumberGenerator.Create(42), searchSpace, sigma).ShouldBe(candidate);
    }

    [Fact]
    public void BoundsMustBeBroadcastableBeforeRandomDraws()
    {
        var integerCandidate = IntegerVector.Create(1, 2);
        var incompatibleIntegerMinimum = IntegerVector.Create(0, 0, 0);
        IntegerVector integerMaximum = 10;
        var realCandidate = RealVector.Create(1, 2);
        var incompatibleRealMinimum = RealVector.Create(0, 0, 0);
        RealVector realMaximum = 10;
        var randomWithoutValues = new SequenceRandomNumberGenerator();

        Should.Throw<ArgumentException>(() => UniformOnePositionMutator.Mutate(integerCandidate, randomWithoutValues, incompatibleIntegerMinimum, integerMaximum))
            .Message.ShouldContain("Minimum and maximum");
        Should.Throw<ArgumentException>(() => UniformSomePositionsMutator.Mutate(integerCandidate, randomWithoutValues, incompatibleIntegerMinimum, integerMaximum, probability: 0.5))
            .Message.ShouldContain("Minimum and maximum");
        Should.Throw<ArgumentException>(() => RoundedNormalOnePositionMutator.Mutate(integerCandidate, randomWithoutValues, incompatibleIntegerMinimum, integerMaximum, sigma: 1))
            .Message.ShouldContain("Minimum, maximum, and sigma");
        Should.Throw<ArgumentException>(() => RoundedNormalAllPositionsMutator.Mutate(integerCandidate, randomWithoutValues, incompatibleIntegerMinimum, integerMaximum, sigma: 1))
            .Message.ShouldContain("Minimum, maximum, and sigma");
        Should.Throw<ArgumentException>(() => GaussianMutator.Mutate(realCandidate, randomWithoutValues, incompatibleRealMinimum, realMaximum, mutationRate: 0.5, mutationStrength: 1))
            .Message.ShouldContain("Minimum and maximum");
        Should.Throw<ArgumentException>(() => PolynomialMutator.Mutate(realCandidate, randomWithoutValues, incompatibleRealMinimum, realMaximum, eta: 20, atLeastOnce: false))
            .Message.ShouldContain("Minimum and maximum");
    }
}
