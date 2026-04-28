using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Tests.SearchSpaces.Vectors;

public class PermutationSearchSpaceTests
{
  [Fact]
  public void Constructor_SetsLength()
  {
    var space = new PermutationSearchSpace(5);

    Assert.Equal(5, space.Length);
  }

  [Fact]
  public void Contains_ReturnsTrue_WhenLengthMatches()
  {
    var space = new PermutationSearchSpace(4);

    int[] values = [0, 1, 2, 3];
    Assert.True(space.Contains(values));
  }

  [Fact]
  public void Contains_ReturnsFalse_WhenLengthIsTooShort()
  {
    var space = new PermutationSearchSpace(4);

    int[] values = [0, 1, 2];
    Assert.False(space.Contains(values));
  }

  [Fact]
  public void Contains_ReturnsFalse_WhenLengthIsTooLong()
  {
    var space = new PermutationSearchSpace(4);

    int[] values = [0, 1, 2, 3, 4];
    Assert.False(space.Contains(values));
  }

  [Fact]
  public void Contains_ReturnsTrue_ForEmptyPermutation_WhenLengthIsZero()
  {
    var space = new PermutationSearchSpace(0);

    Assert.True(space.Contains(Array.Empty<int>()));
  }

  [Fact]
  public void ImplicitConversion_ToIntegerVectorSearchSpace_SetsLengthAndBounds()
  {
    var permutationSpace = new PermutationSearchSpace(5);

    IntegerVectorSearchSpace intSpace = permutationSpace;

    Assert.Equal(5, intSpace.Length);
    Assert.Equal(new IntegerVector(0), intSpace.Minimum);
    Assert.Equal(new IntegerVector(4), intSpace.Maximum);
  }

  [Fact]
  public void ImplicitConversion_ForLengthOne_CreatesSingleValueRange()
  {
    var permutationSpace = new PermutationSearchSpace(1);

    IntegerVectorSearchSpace intSpace = permutationSpace;

    Assert.Equal(1, intSpace.Length);
    Assert.Equal(new IntegerVector(0), intSpace.Minimum);
    Assert.Equal(new IntegerVector(0), intSpace.Maximum);
  }

  [Fact]
  public void ImplicitConversion_ForLengthZero_Throws()
  {
    var permutationSpace = new PermutationSearchSpace(0);

    Assert.Throws<ArgumentException>(() => {
      IntegerVectorSearchSpace _ = permutationSpace;
    });
  }
}
