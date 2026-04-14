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
    int[] values2 = [1, 2, 3];
    var space = new IntegerVectorSearchSpace(
      Length: 3,
      Minimum: values,
      Maximum: values1,
      Step: values2);

    Assert.Equal(3, space.Length);
    int[] values3 = [0, 1, 2];
    Assert.Equal(values3, space.Minimum);
    int[] values4 = [10, 11, 12];
    Assert.Equal(values4, space.Maximum);
    int[] values5 = [1, 2, 3];
    Assert.Equal(values5, space.Step);
  }

  [Fact]
  public void Constructor_UsesDefaultStepOfOne_WhenStepIsNull()
  {
    int[] values = [0];
    int[] values1 = [10];
    var space = new IntegerVectorSearchSpace(
      Length: 3,
      Minimum: values,
      Maximum: values1);

    int[] values2 = [1];
    Assert.Equal(values2, space.Step);
  }

  [Fact]
  public void Constructor_AllowsScalarBounds_ThatBroadcastToAllDimensions()
  {
    var ex = Record.Exception(() => {
      int[] values = [0];
      int[] values1 = [9];
      int[] values2 = [2];
      return new IntegerVectorSearchSpace(
        Length: 4,
        Minimum: values,
        Maximum: values1,
        Step: values2);
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
  public void Constructor_Throws_WhenStepCountIsInvalid()
  {
    Assert.Throws<ArgumentException>(() => {
      int[] values = [0, 0, 0];
      int[] values1 = [10, 10, 10];
      int[] values2 = [1, 2];
      return new IntegerVectorSearchSpace(
        Length: 3,
        Minimum: values,
        Maximum: values1,
        Step: values2);
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

  [Theory]
  [InlineData(0)]
  [InlineData(-1)]
  public void Constructor_Throws_WhenAnyStepIsZeroOrNegative(int invalidStep)
  {
    Assert.Throws<ArgumentException>(() => {
      int[] values = [0, 0, 0];
      int[] values1 = [10, 10, 10];
      int[] values2 = [1, invalidStep, 1];
      return new IntegerVectorSearchSpace(
        Length: 3,
        Minimum: values,
        Maximum: values1,
        Step: values2);
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
    int[] values2 = [2, 2, 2];
    var intSpace = new IntegerVectorSearchSpace(
      Length: 3,
      Minimum: values,
      Maximum: values1,
      Step: values2);

    RealVectorSearchSpace realSpace = intSpace;

    Assert.Equal(3, realSpace.Length);
    Assert.Equal(intSpace.Minimum, realSpace.Minimum);
    Assert.Equal(intSpace.Maximum, realSpace.Maximum);
  }

  [Theory]
  [InlineData(-10.0, 0)]
  [InlineData(0.0, 0)]
  [InlineData(0.1, 0)]
  [InlineData(1.9, 0)]
  [InlineData(2.0, 2)]
  [InlineData(3.9, 2)]
  [InlineData(4.0, 4)]
  [InlineData(10.0, 10)]
  [InlineData(99.0, 10)]
  public void FloorFeasible_ReturnsGreatestFeasibleValueNotGreaterThanX(double x, int expected)
  {
    int[] values = [0];
    int[] values1 = [10];
    int[] values2 = [2];
    var space = new IntegerVectorSearchSpace(
      Length: 1,
      Minimum: values,
      Maximum: values1,
      Step: values2);

    Assert.Equal(expected, space.FloorFeasible(x, 0));
  }

  [Theory]
  [InlineData(-10.0, 0)]
  [InlineData(0.0, 0)]
  [InlineData(0.1, 2)]
  [InlineData(1.9, 2)]
  [InlineData(2.0, 2)]
  [InlineData(2.1, 4)]
  [InlineData(9.9, 10)]
  [InlineData(10.0, 10)]
  [InlineData(99.0, 10)]
  public void CeilingFeasible_ReturnsSmallestFeasibleValueNotLessThanX(double x, int expected)
  {
    int[] values = [0];
    int[] values1 = [10];
    int[] values2 = [2];
    var space = new IntegerVectorSearchSpace(
      Length: 1,
      Minimum: values,
      Maximum: values1,
      Step: values2);

    Assert.Equal(expected, space.CeilingFeasible(x, 0));
  }

  [Theory]
  [InlineData(-10.0, 0)]
  [InlineData(0.0, 0)]
  [InlineData(0.9, 0)]
  [InlineData(1.0, 2)]
  [InlineData(1.9, 2)]
  [InlineData(2.0, 2)]
  [InlineData(2.9, 2)]
  [InlineData(3.0, 4)]
  [InlineData(9.0, 10)]
  [InlineData(10.0, 10)]
  [InlineData(99.0, 10)]
  public void RoundFeasible_ReturnsNearestFeasibleValue(double x, int expected)
  {
    int[] values = [0];
    int[] values1 = [10];
    int[] values2 = [2];
    var space = new IntegerVectorSearchSpace(
      Length: 1,
      Minimum: values,
      Maximum: values1,
      Step: values2);

    Assert.Equal(expected, space.RoundFeasible(x, 0));
  }

  [Fact]
  public void RoundFeasible_Vector_RoundsEachDimensionIndependently()
  {
    int[] values = [0, 10, 100];
    int[] values1 = [10, 20, 110];
    int[] values2 = [2, 5, 3];
    var space = new IntegerVectorSearchSpace(
      Length: 3,
      Minimum: values,
      Maximum: values1,
      Step: values2);

    double[] values4 = [3.1, 17.6, 108.4];
    var rounded = space.RoundFeasible(values4);

    int[] values3 = [4, 20, 109];
    Assert.Equal(values3, rounded);
  }

  [Fact]
  public void FloorFeasible_UsesPerDimensionInformation()
  {
    int[] values = [0, 10, 100];
    int[] values1 = [10, 20, 110];
    int[] values2 = [2, 5, 3];
    var space = new IntegerVectorSearchSpace(
      Length: 3,
      Minimum: values,
      Maximum: values1,
      Step: values2);

    Assert.Equal(0, space.FloorFeasible(1.9, 0));
    Assert.Equal(15, space.FloorFeasible(17.6, 1));
    Assert.Equal(106, space.FloorFeasible(108.4, 2));
  }

  [Fact]
  public void CeilingFeasible_UsesPerDimensionInformation()
  {
    int[] values = [0, 10, 100];
    int[] values1 = [10, 20, 110];
    int[] values2 = [2, 5, 3];
    var space = new IntegerVectorSearchSpace(
      Length: 3,
      Minimum: values,
      Maximum: values1,
      Step: values2);

    Assert.Equal(2, space.CeilingFeasible(1.9, 0));
    Assert.Equal(20, space.CeilingFeasible(17.6, 1));
    Assert.Equal(109, space.CeilingFeasible(108.4, 2));
  }

  [Fact]
  public void RoundFeasible_ClampsBelowMinimum()
  {
    int[] values = [5];
    int[] values1 = [11];
    int[] values2 = [2];
    var space = new IntegerVectorSearchSpace(
      Length: 1,
      Minimum: values,
      Maximum: values1,
      Step: values2);

    Assert.Equal(5, space.RoundFeasible(-100.0, 0));
  }

  [Fact]
  public void RoundFeasible_ClampsAboveMaximum()
  {
    int[] values = [5];
    int[] values1 = [11];
    int[] values2 = [2];
    var space = new IntegerVectorSearchSpace(
      Length: 1,
      Minimum: values,
      Maximum: values1,
      Step: values2);

    Assert.Equal(11, space.RoundFeasible(100.0, 0));
  }

  [Fact]
  public void Contains_ReturnsTrue_ForValueOnStepGrid()
  {
    var space = new IntegerVectorSearchSpace(
      1,
      0,
      10,
      2);

    Assert.True(space.Contains(4));
  }

  [Fact]
  public void Contains_ReturnsFalse_ForValueOffStepGrid()
  {
    var space = new IntegerVectorSearchSpace(
      1,
      0,
      10,
      2);

    Assert.False(space.Contains(3));
  }

  [Fact]
  public void Contains_Works_WithScalarBoundsAndScalarStep()
  {
    var space = new IntegerVectorSearchSpace(
      3,
      0,
      10,
      2);

    Assert.True(space.Contains(new IntegerVector(0, 2, 10)));
    Assert.False(space.Contains(new IntegerVector(0, 3, 10)));
  }

  [Fact]
  public void Contains_Works_WithPerDimensionStep()
  {
    var space = new IntegerVectorSearchSpace(
      3,
      new IntegerVector(0, 10, 100),
      new IntegerVector(10, 20, 110),
      new IntegerVector(2, 5, 3));

    Assert.True(space.Contains(new IntegerVector(4, 15, 109)));
    Assert.False(space.Contains(new IntegerVector(4, 16, 109)));
  }

  [Fact]
  public void Contains_DoesNotThrow_WithScalarBounds()
  {
    var space = new IntegerVectorSearchSpace(
      3,
      0,
      10,
      2);

    var ex = Record.Exception(() => space.Contains(new[] { 0, 2, 4 }));

    Assert.Null(ex);
  }
}
