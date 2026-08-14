using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators.Mutators.IntegerVectorMutators;
using HEAL.HeuristicLib.Operators.Mutators.RealVectorMutators;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Tests.Operators.Mutators;

public sealed class ZeroLengthMutatorTests
{
    [Fact]
    public void IntegerVectorMutators_ReturnOriginalWithoutUsingRandom()
    {
        var candidate = IntegerVector.Create();
        var searchSpace = new IntegerVectorSearchSpace(0, IntegerVector.Create(0), IntegerVector.Create(1));
        var random = new ThrowingRandomNumberGenerator();

        UniformOnePositionMutator.Mutate(candidate, random, searchSpace).ShouldBeSameAs(candidate);
        RoundedNormalOnePositionMutator.Mutate(candidate, random, searchSpace, RealVector.Create(1)).ShouldBeSameAs(candidate);
        RoundedNormalAllPositionsMutator.Mutate(candidate, random, searchSpace, RealVector.Create(1)).ShouldBeSameAs(candidate);
        UniformSomePositionsMutator.Mutate(candidate, random, searchSpace, probability: 1, atLeastOnce: true).ShouldBeSameAs(candidate);
    }

    [Fact]
    public void RealVectorMutators_ReturnOriginalWithoutUsingRandom()
    {
        var candidate = RealVector.Create();
        var searchSpace = new RealVectorSearchSpace(0, minimum: 0, maximum: 1);
        var random = new ThrowingRandomNumberGenerator();

        GaussianMutator.Mutate(candidate, random, searchSpace, mutationRate: 1, mutationStrength: 1).ShouldBeSameAs(candidate);
        PolynomialMutator.Mutate(candidate, random, searchSpace, eta: 20, atLeastOnce: true).ShouldBeSameAs(candidate);
    }

    private sealed class ThrowingRandomNumberGenerator : IRandomNumberGenerator
    {
        public int NextInt() => throw new InvalidOperationException();

        public IRandomNumberGenerator Fork(ulong forkKey) => throw new InvalidOperationException();

        public double NextDouble() => throw new InvalidOperationException();
    }
}
