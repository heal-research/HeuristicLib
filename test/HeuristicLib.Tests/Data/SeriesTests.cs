namespace HEAL.HeuristicLib.Tests.Data;

public sealed class SeriesTests
{
    [Fact]
    public void Constructor_CopiesValuesAndPreservesName()
    {
        var values = new[] { 1.0, 2.0 };

        var series = new Series<double>("x", values);
        values[0] = 3.0;

        series.Name.ShouldBe("x");
        series.Count.ShouldBe(2);
        series.DataType.ShouldBe(typeof(double));
        series.Values.ToArray().ShouldBe([1.0, 2.0]);
    }

    [Fact]
    public void FromOwnedArray_UsesTheSuppliedStorage()
    {
        var values = new[] { 1, 2 };

        var series = Series<int>.FromOwnedArray("category", values);
        values[0] = 3;

        series.Values.ToArray().ShouldBe([3, 2]);
    }

    [Fact]
    public void EqualContents_DoNotImplyValueEquality()
    {
        var first = new Series<double>("x", [1.0]);
        var second = new Series<double>("x", [1.0]);

        ReferenceEquals(first, second).ShouldBeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_RejectsMissingName(string name)
    {
        Should.Throw<ArgumentException>(() => new Series<double>(name, [1.0]));
    }
}
