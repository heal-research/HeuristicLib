using System.Globalization;

namespace HEAL.HeuristicLib.Tests.Data;

public sealed class DataFrameTests
{
    [Fact]
    public void Constructor_PreservesHeterogeneousColumnOrder()
    {
        var x = new Series<double>("x", [1.0, 2.0]);
        var category = new Series<int>("category", [1, 2]);

        var data = new DataFrame([x, category]);

        data.RowCount.ShouldBe(2);
        data.ColumnCount.ShouldBe(2);
        data.Columns.ShouldBe([x, category]);
        data[0].ShouldBeSameAs(x);
        data[1].ShouldBeSameAs(category);
        data["x"].ShouldBeSameAs(x);
        data["category"].ShouldBeSameAs(category);
    }

    [Fact]
    public void Get_ReturnsTheTypedSeries()
    {
        var x = new Series<double>("x", [1.0]);
        var data = new DataFrame([x]);

        data.Get<double>("x").ShouldBeSameAs(x);
        data.TryGet("x", out var untyped).ShouldBeTrue();
        untyped.ShouldBeSameAs(x);
        data.TryGet<double>("x", out var typed).ShouldBeTrue();
        typed.ShouldBeSameAs(x);
    }

    [Fact]
    public void Lookup_ReportsMissingAndMismatchedColumns()
    {
        var data = new DataFrame([new Series<double>("x", [1.0])]);

        Should.Throw<KeyNotFoundException>(() => _ = data["missing"]);
        Should.Throw<InvalidOperationException>(() => data.Get<int>("x"));
        data.TryGet("X", out _).ShouldBeFalse();
        data.TryGet("missing", out _).ShouldBeFalse();
        data.TryGet<int>("x", out _).ShouldBeFalse();
    }

    [Fact]
    public void Constructor_RejectsDifferentSeriesLengths()
    {
        Should.Throw<ArgumentException>(() =>
            new DataFrame([
                new Series<double>("x", [1.0]),
                new Series<int>("category", [1, 2])
            ]));
    }

    [Fact]
    public void Constructor_RejectsDuplicateSeriesNames()
    {
        Should.Throw<ArgumentException>(() =>
            new DataFrame([
                new Series<double>("x", [1.0]),
                new Series<int>("x", [2])
            ]));
    }

    [Fact]
    public void FromMatrix_BuildsNamedSeriesFromMatrixColumns()
    {
        var data = DataFrame.FromMatrix(
            ["x0", "x1"],
            new[,]
            {
                { 1.0, 3.0 },
                { 2.0, 4.0 }
            });

        data.Get<double>("x0").Values.ToArray().ShouldBe([1.0, 2.0]);
        data.Get<double>("x1").Values.ToArray().ShouldBe([3.0, 4.0]);
    }

    [Fact]
    public void FromMatrix_RejectsWrongColumnNameCount()
    {
        Should.Throw<ArgumentException>(() =>
            DataFrame.FromMatrix(["x0"], new[,] { { 1.0, 2.0 } }));
    }

    [Fact]
    public void ReadCsv_InfersColumnTypesAndHandlesQuotedFields()
    {
        using var csv = new TemporaryCsvFile(
            """
            value,count,label,enabled
            1.5,1,"first, value",true
            2.25,2,second,false
            """);

        var data = DataFrame.ReadCsv(csv.Path);

        data.RowCount.ShouldBe(2);
        data.Get<double>("value").Values.ToArray().ShouldBe([1.5, 2.25]);
        data.Get<int>("count").Values.ToArray().ShouldBe([1, 2]);
        data.Get<string>("label").Values.ToArray().ShouldBe(["first, value", "second"]);
        data.Get<bool>("enabled").Values.ToArray().ShouldBe([true, false]);
    }

    [Fact]
    public void ReadCsv_UsesExplicitTypesDelimiterAndCulture()
    {
        using var csv = new TemporaryCsvFile(
            """
            value;group
            1,5;1
            2,75;2
            """);

        var data = DataFrame.ReadCsv(
            csv.Path,
            delimiter: ';',
            dataTypes: [typeof(double), typeof(long)],
            culture: CultureInfo.GetCultureInfo("de-AT"));

        data.Get<double>("value").Values.ToArray().ShouldBe([1.5, 2.75]);
        data.Get<long>("group").Values.ToArray().ShouldBe([1L, 2L]);
    }

    [Fact]
    public void ReadCsv_GeneratesNamesWithoutAHeader()
    {
        using var csv = new TemporaryCsvFile(
            """
            1.0,alpha
            2.0,beta
            """);

        var data = DataFrame.ReadCsv(
            csv.Path,
            hasHeader: false,
            dataTypes: [typeof(double), typeof(string)]);

        data.Columns.Select(x => x.Name).ShouldBe(["Column0", "Column1"]);
        data.Get<double>("Column0").Values.ToArray().ShouldBe([1.0, 2.0]);
        data.Get<string>("Column1").Values.ToArray().ShouldBe(["alpha", "beta"]);
    }

    [Fact]
    public void ReadCsv_RejectsSchemaAndRowWidthMismatches()
    {
        using var csv = new TemporaryCsvFile(
            """
            x,y
            1,2
            3
            """);

        Should.Throw<ArgumentException>(() =>
            DataFrame.ReadCsv(csv.Path, dataTypes: [typeof(double)]));
        Should.Throw<FormatException>(() =>
            DataFrame.ReadCsv(csv.Path, dataTypes: [typeof(double), typeof(double)]));
    }

    [Fact]
    public void ReadCsv_RejectsUnsupportedAndInvalidExplicitTypes()
    {
        using var csv = new TemporaryCsvFile(
            """
            value
            text
            """);

        Should.Throw<NotSupportedException>(() =>
            DataFrame.ReadCsv(csv.Path, dataTypes: [typeof(Guid)]));
        Should.Throw<FormatException>(() =>
            DataFrame.ReadCsv(csv.Path, dataTypes: [typeof(double)]));
    }

    private sealed class TemporaryCsvFile : IDisposable
    {
        public TemporaryCsvFile(string content)
        {
            Path = System.IO.Path.GetTempFileName();
            File.WriteAllText(Path, content);
        }

        public string Path { get; }

        public void Dispose() => File.Delete(Path);
    }
}
