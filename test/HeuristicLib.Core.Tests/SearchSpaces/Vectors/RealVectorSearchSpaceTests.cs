using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Tests.SearchSpaces.Vectors;

public class RealVectorSearchSpaceTests
{
  // ---------------------------
  // Constructor tests
  // ---------------------------

  [Fact]
  public void Constructor_WithScalars_SetsPropertiesCorrectly()
  {
    var space = new RealVectorSearchSpace(3, 0.0, 10.0);

    Assert.Equal(3, space.Length);
    double[] values = [0.0];
    Assert.Equal((RealVector)values, space.Minimum);
    double[] values1 = [10.0];
    Assert.Equal((RealVector)values1, space.Maximum);
  }

  [Fact]
  public void Constructor_WithVectors_SetsPropertiesCorrectly()
  {
    double[] values = [0.0, 1.0, 2.0];
    double[] values1 = [10.0, 11.0, 12.0];
    var space = new RealVectorSearchSpace(
      3,
      values,
      values1);

    Assert.Equal(3, space.Length);
    double[] values2 = [0.0, 1.0, 2.0];
    Assert.Equal((RealVector)values2, space.Minimum);
    double[] values3 = [10.0, 11.0, 12.0];
    Assert.Equal((RealVector)values3, space.Maximum);
  }

  [Fact]
  public void Constructor_AllowsScalarBroadcast()
  {
    var ex = Record.Exception(() => {
      double[] values = [0.0];
      double[] values1 = [10.0];
      return new RealVectorSearchSpace(
        4,
        values,
        values1);
    });

    Assert.Null(ex);
  }

  [Fact]
  public void Constructor_Throws_WhenMinimumIsIncompatible()
  {
    Assert.Throws<ArgumentException>(() => {
      double[] values = [0.0, 1.0];
      double[] values1 = [10.0, 11.0, 12.0];
      return new RealVectorSearchSpace(
        3,
        values,
        values1);
    });
  }

  [Fact]
  public void Constructor_Throws_WhenMaximumIsIncompatible()
  {
    Assert.Throws<ArgumentException>(() => {
      double[] values = [0.0, 1.0, 2.0];
      double[] values1 = [10.0, 11.0];
      return new RealVectorSearchSpace(
        3,
        values,
        values1);
    });
  }

  // ---------------------------
  // Contains tests
  // ---------------------------

  [Fact]
  public void Contains_ReturnsTrue_ForVectorInsideBounds()
  {
    double[] values = [0.0, 1.0, 2.0];
    double[] values1 = [10.0, 11.0, 12.0];
    var space = new RealVectorSearchSpace(
      3,
      values,
      values1);

    double[] values2 = [0.0, 5.5, 12.0];
    Assert.True(space.Contains(values2));
  }

  [Fact]
  public void Contains_ReturnsFalse_WhenLengthDoesNotMatch()
  {
    double[] values = [0.0];
    double[] values1 = [10.0];
    var space = new RealVectorSearchSpace(
      3,
      values,
      values1);

    double[] values2 = [1.0, 2.0];
    Assert.False(space.Contains(values2));
  }

  [Fact]
  public void Contains_ReturnsFalse_WhenValueBelowMinimum()
  {
    double[] values = [0.0, 1.0, 2.0];
    double[] values1 = [10.0, 11.0, 12.0];
    var space = new RealVectorSearchSpace(
      3,
      values,
      values1);

    double[] values2 = [0.0, 0.5, 12.0];
    Assert.False(space.Contains(values2));
  }

  [Fact]
  public void Contains_ReturnsFalse_WhenValueAboveMaximum()
  {
    double[] values = [0.0, 1.0, 2.0];
    double[] values1 = [10.0, 11.0, 12.0];
    var space = new RealVectorSearchSpace(
      3,
      values,
      values1);

    double[] values2 = [0.0, 5.0, 13.0];
    Assert.False(space.Contains(values2));
  }

  [Fact]
  public void Contains_WorksWithScalarBounds()
  {
    var space = new RealVectorSearchSpace(
      3,
      0.0,
      10.0);

    double[] values = [0.0, 5.5, 10.0];
    Assert.True(space.Contains(values));
    double[] values1 = [-1.0, 5.0, 10.0];
    Assert.False(space.Contains(values1));
    double[] values2 = [0.0, 5.0, 11.0];
    Assert.False(space.Contains(values2));
  }

  // ---------------------------
  // Edge / boundary behavior
  // ---------------------------

  [Fact]
  public void Contains_IncludesBoundaryValues()
  {
    double[] values = [0.0, 1.0];
    double[] values1 = [10.0, 11.0];
    var space = new RealVectorSearchSpace(
      2,
      values,
      values1);

    double[] values2 = [0.0, 1.0];
    Assert.True(space.Contains(values2)); // min
    double[] values3 = [10.0, 11.0];
    Assert.True(space.Contains(values3)); // max
  }

  [Fact]
  public void Contains_DoesNotThrow_WithScalarBounds()
  {
    var space = new RealVectorSearchSpace(
      3,
      0.0,
      10.0);

    var ex = Record.Exception(() => {
      double[] values = [1.0, 2.0, 3.0];
      return space.Contains(values);
    });

    Assert.Null(ex);
  }
}
