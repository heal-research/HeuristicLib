using HEAL.HeuristicLib.Encodings.Permutations;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.Tests.TestSupport.Random;

namespace HEAL.HeuristicLib.Tests.Operators.Mutators.PermutationMutators;

public class PermutationMutatorCatalogTests
{
    [Fact]
    public void Mutators_PreserveValuesAndParentsAndAreReproducible()
    {
        SingleCandidateMutator<Permutation>[] mutators = [
            new InsertionMutator(), new ScrambleMutator(), new Swap3Mutator(),
            new TranslocationMutator(), new TranslocationInversionMutator()
        ];
        foreach (var mutator in mutators)
            for (var length = 3; length <= 32; length++)
                for (var seed = 0; seed < 20; seed++)
                {
                    var parent = RandomNumberGenerator.Create(seed).NextPermutation(length);
                    var original = parent.ToArray();

                    var result = mutator.MutateCandidate(parent, RandomNumberGenerator.Create(seed));
                    var repeated = mutator.MutateCandidate(parent, RandomNumberGenerator.Create(seed));

                    result.Order().ShouldBe(Enumerable.Range(0, length));
                    result.ShouldBe(repeated);
                    parent.ToArray().ShouldBe(original);
                }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void SegmentMutators_ReturnShortParentsWithoutDrawingRandomness(int length)
    {
        SingleCandidateMutator<Permutation>[] mutators = [
            new InsertionMutator(), new ScrambleMutator(),
            new TranslocationMutator(), new TranslocationInversionMutator()
        ];
        var parent = Permutation.Range(length);
        foreach (var mutator in mutators)
            mutator.MutateCandidate(parent, new SequenceRandomNumberGenerator()).ShouldBeSameAs(parent);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Swap3_RejectsShortParentsBeforeDrawingRandomness(int length) =>
        Should.Throw<ArgumentException>(() => Swap3Mutator.Mutate(Permutation.Range(length), new SequenceRandomNumberGenerator()));

    [Fact]
    public void Swap3_RotatesThreeDistinctPositionsWithoutRejectionSampling() =>
        Swap3Mutator.Mutate(Permutation.Range(5), new SequenceRandomNumberGenerator(0, 0, 0))
            .ShouldBe(Permutation.Create(1, 2, 0, 3, 4));

    [Fact]
    public void Insertion_MovesOneElementInBothDirections()
    {
        InsertionMutator.Mutate(Permutation.Range(5), new SequenceRandomNumberGenerator(0.2, 0.8))
            .ShouldBe(Permutation.Create(0, 2, 3, 4, 1));
        InsertionMutator.Mutate(Permutation.Range(5), new SequenceRandomNumberGenerator(0.8, 0.2))
            .ShouldBe(Permutation.Create(0, 4, 1, 2, 3));
    }

    [Fact]
    public void Scramble_ChangesOnlyTheSelectedSegment() =>
        ScrambleMutator.Mutate(Permutation.Range(6), new SequenceRandomNumberGenerator(0.2, 0.5, 0, 0, 0))
            .ShouldBe(Permutation.Create(0, 2, 3, 4, 1, 5));

    [Fact]
    public void Translocation_MovesASegmentInRemainingPermutationCoordinates()
    {
        var parent = Permutation.Range(7);
        TranslocationMutator.Mutate(parent, 1, 3, 4).ShouldBe(Permutation.Create(0, 4, 5, 6, 1, 2, 3));
        TranslocationMutator.Mutate(parent, 3, 5, 0).ShouldBe(Permutation.Create(3, 4, 5, 0, 1, 2, 6));
        TranslocationInversionMutator.Mutate(parent, 1, 3, 4).ShouldBe(Permutation.Create(0, 4, 5, 6, 3, 2, 1));
        TranslocationInversionMutator.Mutate(parent, 0, 6, 0).ShouldBe(Permutation.Create(6, 5, 4, 3, 2, 1, 0));
    }

    [Fact]
    public void Translocation_CanSampleTheFinalInsertionPoint()
    {
        var parent = Permutation.Range(7);
        TranslocationMutator.Mutate(parent, new SequenceRandomNumberGenerator(0.2, 0.2, 0.99))
            .ShouldBe(Permutation.Create(0, 4, 5, 6, 1, 2, 3));
        TranslocationInversionMutator.Mutate(parent, new SequenceRandomNumberGenerator(0.2, 0.2, 0.99))
            .ShouldBe(Permutation.Create(0, 4, 5, 6, 3, 2, 1));
    }

    [Theory]
    [InlineData(-1, 2, 0)]
    [InlineData(2, 1, 0)]
    [InlineData(1, 7, 0)]
    [InlineData(1, 3, -1)]
    [InlineData(1, 3, 5)]
    public void Translocation_RejectsInvalidSegmentOrInsertionPoint(int start, int end, int insertPoint)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => TranslocationMutator.Mutate(Permutation.Range(7), start, end, insertPoint));
        Should.Throw<ArgumentOutOfRangeException>(() => TranslocationInversionMutator.Mutate(Permutation.Range(7), start, end, insertPoint));
    }
}
