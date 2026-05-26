using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

namespace HEAL.HeuristicLib.Tests.Genotypes.SymbolicExpressions;

public sealed class DataFrameTests
{
    [Fact]
    public void FromColumns_UsesExistingSeries()
    {
        var x0 = Series<double>.Create([1.0, 2.0]);
        var x1 = Series<double>.Create([3.0, 4.0]);

        var data = DataFrame.FromColumns([
          KeyValuePair.Create("x0", x0),
          KeyValuePair.Create("x1", x1)
        ]);

        data.RowCount.ShouldBe(2);
        data.GetDoubleSeries("x0").ShouldBeSameAs(x0);
        data.GetDoubleSeries("x1").ShouldBeSameAs(x1);
    }

    [Fact]
    public void FromOwnedColumns_UsesOwnedArraysAsBackingStorage()
    {
        var x0 = new[] { 1.0, 2.0 };
        var x1 = new[] { 3.0, 4.0 };

        var data = DataFrame.FromOwnedColumns([
          KeyValuePair.Create("x0", x0),
          KeyValuePair.Create("x1", x1)
        ]);

        x0[0] = 5.0;

        data.RowCount.ShouldBe(2);
        data.GetDoubleSeries("x0").Values.ToArray().ShouldBe([5.0, 2.0]);
        data.GetDoubleSeries("x1").Values.ToArray().ShouldBe([3.0, 4.0]);
    }

    [Fact]
    public void FromMatrix_BuildsNamedDoubleSeriesFromMatrixColumns()
    {
        var data = DataFrame.FromMatrix(
          ["x0", "x1"],
          new double[,]
          {
              { 1.0, 3.0 },
              { 2.0, 4.0 }
          });

        data.RowCount.ShouldBe(2);
        data.GetDoubleSeries("x0").Values.ToArray().ShouldBe([1.0, 2.0]);
        data.GetDoubleSeries("x1").Values.ToArray().ShouldBe([3.0, 4.0]);
    }

    [Fact]
    public void FromMatrix_RejectsWrongColumnNameCount()
    {
        Should.Throw<ArgumentException>(() =>
          DataFrame.FromMatrix(["x0"], new double[,] { { 1.0, 2.0 } }));
    }

    [Fact]
    public void Constructor_RejectsDifferentSeriesLengths()
    {
        Should.Throw<ArgumentException>(() =>
          new DataFrame([
            KeyValuePair.Create("x0", Series<double>.Create([1.0])),
            KeyValuePair.Create("x1", Series<double>.Create([1.0, 2.0]))
          ]));
    }

    [Fact]
    public void Constructor_RejectsDuplicateSeriesNames()
    {
        Should.Throw<ArgumentException>(() =>
          new DataFrame([
            KeyValuePair.Create("x0", Series<double>.Create([1.0])),
            KeyValuePair.Create("x0", Series<double>.Create([2.0]))
          ]));
    }

    [Fact]
    public void GetDoubleSeries_RejectsUnknownName()
    {
        var data = new DataFrame([
          KeyValuePair.Create("x0", Series<double>.Create([1.0]))
        ]);

        Should.Throw<ArgumentException>(() => data.GetDoubleSeries("x1"));
    }

    [Fact]
    public void SeriesCreate_CopiesInput()
    {
        var values = new[] { 1.0 };
        var series = Series<double>.Create(values);

        values[0] = 2.0;

        series.Values.ToArray().ShouldBe([1.0]);
    }
}
