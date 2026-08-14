namespace HEAL.HeuristicLib.Tests;

public sealed class EnumerableExtensionsTests
{
    [Fact]
    public void Range_ReturnsDifferenceBetweenMaximumAndMinimum()
    {
        new[] { 4.0, 1.0, 3.0, 2.0 }.Range().ShouldBe(3.0);
    }

    [Fact]
    public void Range_SingleValue_ReturnsZero()
    {
        new[] { 4.0 }.Range().ShouldBe(0.0);
    }

    [Fact]
    public void Range_NaNValue_ReturnsNaN()
    {
        double.IsNaN(new[] { 1.0, double.NaN, 3.0 }.Range()).ShouldBeTrue();
    }

    [Fact]
    public void Range_EmptySequence_Throws()
    {
        Should.Throw<InvalidOperationException>(() => Array.Empty<double>().Range());
    }
}
