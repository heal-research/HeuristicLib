using HEAL.HeuristicLib.Encodings.IntegerVectors;
using HEAL.HeuristicLib.Encodings.RealVectors;

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
          Minimum: IntegerVector.Create(values),
          Maximum: IntegerVector.Create(values1));

        space.Length.ShouldBe(3);
        int[] values3 = [0, 1, 2];
        space.Minimum.ShouldBe(values3);
        int[] values4 = [10, 11, 12];
        space.Maximum.ShouldBe(values4);
    }

    [Fact]
    public void Constructor_AllowsScalarBounds_ThatBroadcastToAllDimensions()
    {
        var ex = Record.Exception(() =>
        {
            int[] values = [0];
            int[] values1 = [9];
            return new IntegerVectorSearchSpace(
              Length: 4,
              Minimum: IntegerVector.Create(values),
              Maximum: IntegerVector.Create(values1));
        });

        ex.ShouldBeNull();
    }

    [Fact]
    public void Constructor_Throws_WhenMinimumCountIsInvalid()
    {
        Should.Throw<ArgumentException>(() =>
        {
            int[] values = [0, 1];
            int[] values1 = [10, 10, 10];
            return new IntegerVectorSearchSpace(
              Length: 3,
              Minimum: IntegerVector.Create(values),
              Maximum: IntegerVector.Create(values1));
        });
    }

    [Fact]
    public void Constructor_Throws_WhenMaximumCountIsInvalid()
    {
        Should.Throw<ArgumentException>(() =>
        {
            int[] values = [0, 0, 0];
            int[] values1 = [10, 10];
            return new IntegerVectorSearchSpace(
              Length: 3,
              Minimum: IntegerVector.Create(values),
              Maximum: IntegerVector.Create(values1));
        });
    }

    [Fact]
    public void Constructor_AllowsMinimumGreaterThanMaximum()
    {
        var space = new IntegerVectorSearchSpace(
          Length: 3,
          Minimum: IntegerVector.Create(0, 5, 0),
          Maximum: IntegerVector.Create(10, 4, 10));

        space.Minimum.ShouldBe(IntegerVector.Create(0, 5, 0));
        space.Maximum.ShouldBe(IntegerVector.Create(10, 4, 10));
    }

    [Fact]
    public void Contains_ReturnsTrue_ForVectorInsideBoundsWithCorrectLength()
    {
        int[] values = [0, 1, 2];
        int[] values1 = [10, 11, 12];
        var space = new IntegerVectorSearchSpace(
          Length: 3,
          Minimum: IntegerVector.Create(values),
          Maximum: IntegerVector.Create(values1));

        int[] values2 = [0, 5, 12];
        space.Contains(IntegerVector.Create(values2)).ShouldBeTrue();
    }

    [Fact]
    public void Contains_ReturnsFalse_WhenLengthDoesNotMatch()
    {
        int[] values = [0];
        int[] values1 = [10];
        var space = new IntegerVectorSearchSpace(
          Length: 3,
          Minimum: IntegerVector.Create(values),
          Maximum: IntegerVector.Create(values1));

        int[] values2 = [1, 2];
        space.Contains(IntegerVector.Create(values2)).ShouldBeFalse();
    }

    [Fact]
    public void Contains_ReturnsFalse_WhenAnyValueIsBelowMinimum()
    {
        int[] values = [0, 1, 2];
        int[] values1 = [10, 11, 12];
        var space = new IntegerVectorSearchSpace(
          Length: 3,
          Minimum: IntegerVector.Create(values),
          Maximum: IntegerVector.Create(values1));

        int[] values2 = [0, 0, 12];
        space.Contains(IntegerVector.Create(values2)).ShouldBeFalse();
    }

    [Fact]
    public void Contains_ReturnsFalse_WhenAnyValueIsAboveMaximum()
    {
        int[] values = [0, 1, 2];
        int[] values1 = [10, 11, 12];
        var space = new IntegerVectorSearchSpace(
          Length: 3,
          Minimum: IntegerVector.Create(values),
          Maximum: IntegerVector.Create(values1));

        int[] values2 = [0, 5, 13];
        space.Contains(IntegerVector.Create(values2)).ShouldBeFalse();
    }

    [Fact]
    public void ImplicitConversion_ToRealVectorSearchSpace_PreservesLengthAndBounds()
    {
        int[] values = [0, 1, 2];
        int[] values1 = [10, 11, 12];
        var intSpace = new IntegerVectorSearchSpace(
          Length: 3,
          Minimum: IntegerVector.Create(values),
          Maximum: IntegerVector.Create(values1));

        RealVectorSearchSpace realSpace = intSpace;

        realSpace.Length.ShouldBe(3);
        realSpace.Minimum.ShouldBe(intSpace.Minimum);
        realSpace.Maximum.ShouldBe(intSpace.Maximum);
    }

    [Fact]
    public void Contains_Works_WithScalarBounds()
    {
        var space = new IntegerVectorSearchSpace(
          3,
          0,
          10);

        space.Contains(new IntegerVector(0, 2, 10)).ShouldBeTrue();
        space.Contains(new IntegerVector(0, 11, 10)).ShouldBeFalse();
    }

    [Fact]
    public void Contains_Works_WithPerDimensionBounds()
    {
        var space = new IntegerVectorSearchSpace(
          3,
          new IntegerVector(0, 10, 100),
          new IntegerVector(10, 20, 110));

        space.Contains(new IntegerVector(4, 16, 109)).ShouldBeTrue();
        space.Contains(new IntegerVector(4, 21, 109)).ShouldBeFalse();
    }

    [Fact]
    public void Contains_DoesNotThrow_WithScalarBounds()
    {
        var space = new IntegerVectorSearchSpace(
          3,
          0,
          10);

        var ex = Record.Exception(() => space.Contains(IntegerVector.Create(0, 2, 4)));

        ex.ShouldBeNull();
    }
}
