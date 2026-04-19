using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Tests.SearchSpaces.Vectors;

public class IntegerVectorSearchSpaceTests
{
  [Fact]
  public void Constructor_SetsProperties_WhenArgumentsAreValid()
  {
    int[] values = [0, 1, 2];
    int[] values1 = [10, 11, 12];
    var space = new IntegerVectorSearchSpace(
      Length: 3,
      Minimum: values,
      Maximum: values1);

    Assert.Equal(3, space.Length);
    int[] values3 = [0, 1, 2];
    Assert.Equal(values3, space.Minimum);
    int[] values4 = [10, 11, 12];
    Assert.Equal(values4, space.Maximum);
  }

  [Fact]
  public void Constructor_AllowsScalarBounds_ThatBroadcastToAllDimensions()
  {
    var ex = Record.Exception(() => {
      int[] values = [0];
      int[] values1 = [9];
      return new IntegerVectorSearchSpace(
        Length: 4,
        Minimum: values,
        Maximum: values1);
    });

    Assert.Null(ex);
  }

  [Fact]
  public void Constructor_Throws_WhenMinimumCountIsInvalid()
  {
    Assert.Throws<ArgumentException>(() => {
      int[] values = [0, 1];
      int[] values1 = [10, 10, 10];
      return new IntegerVectorSearchSpace(
        Length: 3,
        Minimum: values,
        Maximum: values1);
    });
  }

  [Fact]
  public void Constructor_Throws_WhenMaximumCountIsInvalid()
  {
    Assert.Throws<ArgumentException>(() => {
      int[] values = [0, 0, 0];
      int[] values1 = [10, 10];
      return new IntegerVectorSearchSpace(
        Length: 3,
        Minimum: values,
        Maximum: values1);
    });
  }

  [Fact]
  public void Constructor_Throws_WhenMinimumIsGreaterThanMaximum()
  {
    Assert.Throws<ArgumentException>(() => {
      int[] values = [0, 5, 0];
      int[] values1 = [10, 4, 10];
      return new IntegerVectorSearchSpace(
        Length: 3,
        Minimum: values,
        Maximum: values1);
    });
  }

  [Fact]
  public void Contains_ReturnsTrue_ForVectorInsideBoundsWithCorrectLength()
  {
    int[] values = [0, 1, 2];
    int[] values1 = [10, 11, 12];
    var space = new IntegerVectorSearchSpace(
      Length: 3,
      Minimum: values,
      Maximum: values1);

    int[] values2 = [0, 5, 12];
    Assert.True(space.Contains(values2));
  }

  [Fact]
  public void Contains_ReturnsFalse_WhenLengthDoesNotMatch()
  {
    int[] values = [0];
    int[] values1 = [10];
    var space = new IntegerVectorSearchSpace(
      Length: 3,
      Minimum: values,
      Maximum: values1);

    int[] values2 = [1, 2];
    Assert.False(space.Contains(values2));
  }

  [Fact]
  public void Contains_ReturnsFalse_WhenAnyValueIsBelowMinimum()
  {
    int[] values = [0, 1, 2];
    int[] values1 = [10, 11, 12];
    var space = new IntegerVectorSearchSpace(
      Length: 3,
      Minimum: values,
      Maximum: values1);

    int[] values2 = [0, 0, 12];
    Assert.False(space.Contains(values2));
  }

  [Fact]
  public void Contains_ReturnsFalse_WhenAnyValueIsAboveMaximum()
  {
    int[] values = [0, 1, 2];
    int[] values1 = [10, 11, 12];
    var space = new IntegerVectorSearchSpace(
      Length: 3,
      Minimum: values,
      Maximum: values1);

    int[] values2 = [0, 5, 13];
    Assert.False(space.Contains(values2));
  }

  [Fact]
  public void ImplicitConversion_ToRealVectorSearchSpace_PreservesLengthAndBounds()
  {
    int[] values = [0, 1, 2];
    int[] values1 = [10, 11, 12];
    var intSpace = new IntegerVectorSearchSpace(
      Length: 3,
      Minimum: values,
      Maximum: values1);

    RealVectorSearchSpace realSpace = intSpace;

    Assert.Equal(3, realSpace.Length);
    Assert.Equal(intSpace.Minimum, realSpace.Minimum);
    Assert.Equal(intSpace.Maximum, realSpace.Maximum);
  }

  [Fact]
  public void Contains_Works_WithScalarBounds()
  {
    var space = new IntegerVectorSearchSpace(
      3,
      0,
      10);

    Assert.True(space.Contains(new IntegerVector(0, 2, 10)));
    Assert.False(space.Contains(new IntegerVector(0, 11, 10)));
  }

  [Fact]
  public void Contains_Works_WithPerDimensionBounds()
  {
    var space = new IntegerVectorSearchSpace(
      3,
      new IntegerVector(0, 10, 100),
      new IntegerVector(10, 20, 110));

    Assert.True(space.Contains(new IntegerVector(4, 16, 109)));
    Assert.False(space.Contains(new IntegerVector(4, 21, 109)));
  }

  [Fact]
  public void Contains_DoesNotThrow_WithScalarBounds()
  {
    var space = new IntegerVectorSearchSpace(
      3,
      0,
      10);

    var ex = Record.Exception(() => space.Contains(new[] { 0, 2, 4 }));

    Assert.Null(ex);
  }
}
