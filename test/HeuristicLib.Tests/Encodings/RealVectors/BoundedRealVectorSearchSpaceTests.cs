using HEAL.HeuristicLib.Encodings.RealVectors;

namespace HEAL.HeuristicLib.Tests.SearchSpaces.Vectors;

public class BoundedRealVectorSearchSpaceTests
{
    // ---------------------------
    // Constructor tests
    // ---------------------------

    [Fact]
    public void Constructor_WithScalars_SetsPropertiesCorrectly()
    {
        var space = new BoundedRealVectorSearchSpace(3, 0.0, 10.0);

        space.Length.ShouldBe(3);
        double[] values = [0.0];
        space.Minimum.ShouldBe(RealVector.Create(values));
        double[] values1 = [10.0];
        space.Maximum.ShouldBe(RealVector.Create(values1));
    }

    [Fact]
    public void Constructor_WithVectors_SetsPropertiesCorrectly()
    {
        double[] values = [0.0, 1.0, 2.0];
        double[] values1 = [10.0, 11.0, 12.0];
        var space = new BoundedRealVectorSearchSpace(
          3,
          RealVector.Create(values),
          RealVector.Create(values1));

        space.Length.ShouldBe(3);
        double[] values2 = [0.0, 1.0, 2.0];
        space.Minimum.ShouldBe(RealVector.Create(values2));
        double[] values3 = [10.0, 11.0, 12.0];
        space.Maximum.ShouldBe(RealVector.Create(values3));
    }

    [Fact]
    public void Constructor_AllowsScalarBroadcast()
    {
        var ex = Record.Exception(() =>
        {
            double[] values = [0.0];
            double[] values1 = [10.0];
            return new BoundedRealVectorSearchSpace(
              4,
              RealVector.Create(values),
              RealVector.Create(values1));
        });

        ex.ShouldBeNull();
    }

    [Fact]
    public void Constructor_Throws_WhenMinimumIsIncompatible()
    {
        Should.Throw<ArgumentException>(() =>
        {
            double[] values = [0.0, 1.0];
            double[] values1 = [10.0, 11.0, 12.0];
            return new BoundedRealVectorSearchSpace(
              3,
              RealVector.Create(values),
              RealVector.Create(values1));
        });
    }

    [Fact]
    public void Constructor_Throws_WhenMaximumIsIncompatible()
    {
        Should.Throw<ArgumentException>(() =>
        {
            double[] values = [0.0, 1.0, 2.0];
            double[] values1 = [10.0, 11.0];
            return new BoundedRealVectorSearchSpace(
              3,
              RealVector.Create(values),
              RealVector.Create(values1));
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
        var space = new BoundedRealVectorSearchSpace(
          3,
          RealVector.Create(values),
          RealVector.Create(values1));

        double[] values2 = [0.0, 5.5, 12.0];
        space.Contains(RealVector.Create(values2)).ShouldBeTrue();
    }

    [Fact]
    public void Contains_ReturnsFalse_WhenLengthDoesNotMatch()
    {
        double[] values = [0.0];
        double[] values1 = [10.0];
        var space = new BoundedRealVectorSearchSpace(
          3,
          RealVector.Create(values),
          RealVector.Create(values1));

        double[] values2 = [1.0, 2.0];
        space.Contains(RealVector.Create(values2)).ShouldBeFalse();
    }

    [Fact]
    public void Contains_ReturnsFalse_WhenValueBelowMinimum()
    {
        double[] values = [0.0, 1.0, 2.0];
        double[] values1 = [10.0, 11.0, 12.0];
        var space = new BoundedRealVectorSearchSpace(
          3,
          RealVector.Create(values),
          RealVector.Create(values1));

        double[] values2 = [0.0, 0.5, 12.0];
        space.Contains(RealVector.Create(values2)).ShouldBeFalse();
    }

    [Fact]
    public void Contains_ReturnsFalse_WhenValueAboveMaximum()
    {
        double[] values = [0.0, 1.0, 2.0];
        double[] values1 = [10.0, 11.0, 12.0];
        var space = new BoundedRealVectorSearchSpace(
          3,
          RealVector.Create(values),
          RealVector.Create(values1));

        double[] values2 = [0.0, 5.0, 13.0];
        space.Contains(RealVector.Create(values2)).ShouldBeFalse();
    }

    [Fact]
    public void Contains_WorksWithScalarBounds()
    {
        var space = new BoundedRealVectorSearchSpace(
          3,
          0.0,
          10.0);

        double[] values = [0.0, 5.5, 10.0];
        space.Contains(RealVector.Create(values)).ShouldBeTrue();
        double[] values1 = [-1.0, 5.0, 10.0];
        space.Contains(RealVector.Create(values1)).ShouldBeFalse();
        double[] values2 = [0.0, 5.0, 11.0];
        space.Contains(RealVector.Create(values2)).ShouldBeFalse();
    }

    // ---------------------------
    // Edge / boundary behavior
    // ---------------------------

    [Fact]
    public void Contains_IncludesBoundaryValues()
    {
        double[] values = [0.0, 1.0];
        double[] values1 = [10.0, 11.0];
        var space = new BoundedRealVectorSearchSpace(
          2,
          RealVector.Create(values),
          RealVector.Create(values1));

        double[] values2 = [0.0, 1.0];
        space.Contains(RealVector.Create(values2)).ShouldBeTrue(); // min
        double[] values3 = [10.0, 11.0];
        space.Contains(RealVector.Create(values3)).ShouldBeTrue(); // max
    }

    [Fact]
    public void Contains_DoesNotThrow_WithScalarBounds()
    {
        var space = new BoundedRealVectorSearchSpace(
          3,
          0.0,
          10.0);

        var ex = Record.Exception(() =>
        {
            double[] values = [1.0, 2.0, 3.0];
            return space.Contains(RealVector.Create(values));
        });

        ex.ShouldBeNull();
    }
}
