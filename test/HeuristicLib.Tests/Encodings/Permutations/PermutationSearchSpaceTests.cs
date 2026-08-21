using HEAL.HeuristicLib.Encodings.IntegerVectors;
using HEAL.HeuristicLib.Encodings.Permutations;

namespace HEAL.HeuristicLib.Tests.SearchSpaces.Vectors;

public class PermutationSearchSpaceTests
{
    [Fact]
    public void Constructor_SetsLength()
    {
        var space = new PermutationSearchSpace(5);

        space.Length.ShouldBe(5);
    }

    [Fact]
    public void Contains_ReturnsTrue_WhenLengthMatches()
    {
        var space = new PermutationSearchSpace(4);

        int[] values = [0, 1, 2, 3];
        space.Contains(Permutation.Create(values)).ShouldBeTrue();
    }

    [Fact]
    public void Contains_ReturnsFalse_WhenLengthIsTooShort()
    {
        var space = new PermutationSearchSpace(4);

        int[] values = [0, 1, 2];
        space.Contains(Permutation.Create(values)).ShouldBeFalse();
    }

    [Fact]
    public void Contains_ReturnsFalse_WhenLengthIsTooLong()
    {
        var space = new PermutationSearchSpace(4);

        int[] values = [0, 1, 2, 3, 4];
        space.Contains(Permutation.Create(values)).ShouldBeFalse();
    }

    [Fact]
    public void Contains_ReturnsTrue_ForEmptyPermutation_WhenLengthIsZero()
    {
        var space = new PermutationSearchSpace(0);

        space.Contains(Permutation.Create()).ShouldBeTrue();
    }

    [Fact]
    public void ImplicitConversion_ToIntegerVectorSearchSpace_SetsLengthAndBounds()
    {
        var permutationSpace = new PermutationSearchSpace(5);

        IntegerVectorSearchSpace intSpace = permutationSpace;

        intSpace.Length.ShouldBe(5);
        intSpace.Minimum.ShouldBe(new IntegerVector(0));
        intSpace.Maximum.ShouldBe(new IntegerVector(4));
    }

    [Fact]
    public void ImplicitConversion_ForLengthOne_CreatesSingleValueRange()
    {
        var permutationSpace = new PermutationSearchSpace(1);

        IntegerVectorSearchSpace intSpace = permutationSpace;

        intSpace.Length.ShouldBe(1);
        intSpace.Minimum.ShouldBe(new IntegerVector(0));
        intSpace.Maximum.ShouldBe(new IntegerVector(0));
    }

    [Fact]
    public void ImplicitConversion_ForLengthZero_CreatesCollapsedRange()
    {
        var permutationSpace = new PermutationSearchSpace(0);

        IntegerVectorSearchSpace intSpace = permutationSpace;

        intSpace.Length.ShouldBe(0);
        intSpace.Minimum.ShouldBe(new IntegerVector(0));
        intSpace.Maximum.ShouldBe(new IntegerVector(-1));
    }
}
