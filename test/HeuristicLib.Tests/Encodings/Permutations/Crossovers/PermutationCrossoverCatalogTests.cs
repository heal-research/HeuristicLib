using HEAL.HeuristicLib.Encodings.Permutations;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.Tests.TestSupport.Random;

namespace HEAL.HeuristicLib.Tests.Operators.Crossovers.PermutationCrossovers;

public class PermutationCrossoverCatalogTests
{
    public static TheoryData<string> CrossoverNames => new()
    {
        nameof(CosaCrossover), nameof(CyclicCrossover), nameof(CyclicCrossover2),
        nameof(MaximalPreservativeCrossover), nameof(OrderBasedCrossover), nameof(OrderCrossover2),
        nameof(PartiallyMatchedCrossover), nameof(PositionBasedCrossover), nameof(UniformLikeCrossover)
    };

    [Theory]
    [MemberData(nameof(CrossoverNames))]
    public void Cross_PreservesValuesAndParentsAndIsReproducible(string name)
    {
        var crossover = CreateCrossover(name);
        for (var length = 4; length <= 32; length++)
        {
            for (var seed = 0; seed < 20; seed++)
            {
                var random = RandomNumberGenerator.Create(seed);
                var parent1 = random.NextPermutation(length);
                var parent2 = random.NextPermutation(length);
                var original1 = parent1.ToArray();
                var original2 = parent2.ToArray();
                var parents = new Parents<Permutation>(parent1, parent2);

                var child = crossover.CrossParents(parents, RandomNumberGenerator.Create(seed));
                var repeated = crossover.CrossParents(parents, RandomNumberGenerator.Create(seed));

                child.Order().ShouldBe(Enumerable.Range(0, length));
                child.ShouldBe(repeated);
                parent1.ToArray().ShouldBe(original1);
                parent2.ToArray().ShouldBe(original2);
            }
        }
    }

    [Theory]
    [MemberData(nameof(CrossoverNames))]
    public void Cross_HandlesShortPermutationsBeforeDrawingRandomness(string name)
    {
        var crossover = CreateCrossover(name);
        for (var length = 0; length < 4; length++)
        {
            var parent = Permutation.Range(length);
            var parents = new Parents<Permutation>(parent, parent);
            if (name is nameof(MaximalPreservativeCrossover) or nameof(PartiallyMatchedCrossover))
                Should.Throw<ArgumentException>(() => crossover.CrossParents(parents, new SequenceRandomNumberGenerator()));
            else if (length < 2)
                crossover.CrossParents(parents, new SequenceRandomNumberGenerator()).ShouldBeSameAs(parent);
            else
                crossover.CrossParents(parents, RandomNumberGenerator.Create(1)).ShouldBe(parent);
        }
    }

    [Fact]
    public void CyclicVariants_CopyAlternatingCyclesOrOneSelectedCycle()
    {
        var parent1 = Permutation.Range(8);
        Permutation parent2 = [1, 0, 3, 2, 5, 4, 7, 6];

        CyclicCrossover.Cross(parent1, parent2, new SequenceRandomNumberGenerator(0.9))
            .ShouldBe(Permutation.Create(0, 1, 3, 2, 4, 5, 7, 6));
        CyclicCrossover2.Cross(parent1, parent2, new SequenceRandomNumberGenerator(0.3))
            .ShouldBe(Permutation.Create(1, 0, 2, 3, 5, 4, 7, 6));
    }

    [Fact]
    public void Cosa_IntroducesTheSecondParentsSuccessorByReversingTheFirstParent()
    {
        Permutation parent2 = [1, 0, 3, 2, 5, 4, 7, 6];

        CosaCrossover.Cross(Permutation.Range(8), parent2, new SequenceRandomNumberGenerator(0))
            .ShouldBe(Permutation.Create(0, 3, 2, 1, 4, 5, 6, 7));
    }

    [Fact]
    public void Cosa_ReversesASegmentAcrossThePermutationBoundary()
    {
        Permutation parent2 = [3, 7, 5, 1, 6, 0, 2, 4];

        CosaCrossover.Cross(Permutation.Range(8), parent2, new SequenceRandomNumberGenerator(0.8))
            .ShouldBe(Permutation.Create(7, 1, 2, 3, 4, 5, 6, 0));
    }

    [Fact]
    public void ExistingCrossovers_ReturnShortParentsWithoutDrawingRandomness()
    {
        for (var length = 0; length < 2; length++)
        {
            var parent = Permutation.Range(length);
            OrderCrossover.Cross(parent, parent, new SequenceRandomNumberGenerator()).ShouldBeSameAs(parent);
            EdgeRecombinationCrossover.Cross(parent, parent, new SequenceRandomNumberGenerator()).ShouldBeSameAs(parent);
        }
    }

    [Fact]
    public void Order2_FillsFromTheBeginningOfTheSecondParent()
    {
        Permutation parent2 = [1, 0, 3, 2, 5, 4, 7, 6];

        OrderCrossover2.Cross(Permutation.Range(8), parent2, new SequenceRandomNumberGenerator(0.3, 0.4))
            .ShouldBe(Permutation.Create(1, 0, 2, 3, 4, 5, 7, 6));
    }

    [Fact]
    public void PartiallyMatched_ExchangesMappedValues()
    {
        Permutation parent2 = [3, 7, 5, 1, 6, 0, 2, 4];

        PartiallyMatchedCrossover.Cross(Permutation.Range(8), parent2, new SequenceRandomNumberGenerator(0.3, 0))
            .ShouldBe(Permutation.Create(1, 7, 2, 3, 6, 0, 5, 4));
    }

    [Fact]
    public void PartiallyMatched_SamplesValidEndpointsWithoutRejection()
    {
        Permutation parent2 = [3, 2, 1, 0];

        PartiallyMatchedCrossover.Cross(Permutation.Range(4), parent2, new SequenceRandomNumberGenerator(0, 0.99))
            .ShouldBe(Permutation.Range(4));
    }

    [Fact]
    public void PositionBased_KeepsMaskedPositionsAndFillsGapsInFirstParentOrder()
    {
        Permutation parent2 = [3, 7, 5, 1, 6, 0, 2, 4];

        PositionBasedCrossover.Cross(Permutation.Range(8), parent2,
                new SequenceRandomNumberGenerator(0.5, 0, 0.25, 0.5, 0.75))
            .ShouldBe(Permutation.Create(3, 0, 5, 1, 6, 4, 2, 7));
    }

    [Fact]
    public void OrderBased_ReordersSelectedValuesWithinTheSecondParent()
    {
        Permutation parent2 = [3, 7, 5, 1, 6, 0, 2, 4];

        OrderBasedCrossover.Cross(Permutation.Range(8), parent2, new SequenceRandomNumberGenerator(0.4, 0, 0, 0))
            .ShouldBe(Permutation.Create(3, 7, 5, 0, 6, 1, 2, 4));
    }

    [Fact]
    public void UniformLike_RepairsConflictingChoicesUsingUnusedValues()
    {
        Permutation parent2 = [1, 0, 3, 2];

        UniformLikeCrossover.Cross(Permutation.Range(4), parent2, new SequenceRandomNumberGenerator(0.9, 0.1))
            .ShouldBe(Permutation.Create(1, 0, 2, 3));

        Permutation conflictingParent = [2, 3, 1, 0];
        UniformLikeCrossover.Cross(Permutation.Range(4), conflictingParent,
                new SequenceRandomNumberGenerator(0.9, 0.1, 0.9, 0))
            .ShouldBe(Permutation.Create(2, 1, 3, 0));
    }

    [Fact]
    public void MaximalPreservative_KeepsACyclicSegmentAndPrefersSecondParentSuccessors()
    {
        Permutation parent2 = [0, 1, 2, 4, 3, 5, 6, 7];

        MaximalPreservativeCrossover.Cross(Permutation.Range(8), parent2, new SequenceRandomNumberGenerator(0, 0))
            .ShouldBe(Permutation.Create(0, 1, 2, 4, 3, 5, 6, 7));
    }

    private static SingleCandidateCrossover<Permutation> CreateCrossover(string name) => name switch
    {
        nameof(CosaCrossover) => new CosaCrossover(),
        nameof(CyclicCrossover) => new CyclicCrossover(),
        nameof(CyclicCrossover2) => new CyclicCrossover2(),
        nameof(MaximalPreservativeCrossover) => new MaximalPreservativeCrossover(),
        nameof(OrderBasedCrossover) => new OrderBasedCrossover(),
        nameof(OrderCrossover2) => new OrderCrossover2(),
        nameof(PartiallyMatchedCrossover) => new PartiallyMatchedCrossover(),
        nameof(PositionBasedCrossover) => new PositionBasedCrossover(),
        nameof(UniformLikeCrossover) => new UniformLikeCrossover(),
        _ => throw new ArgumentException("Unknown crossover.", nameof(name))
    };
}
