namespace HEAL.HeuristicLib.Tests.Numerics;

public sealed class ScratchSpansTests
{
    [Fact]
    public void NoneHasNoSpans() => ScratchSpans.None.Count.ShouldBe(0);

    [Fact]
    public void SpansAreSlicedByTheirLength()
    {
        Span<double> buffer = [1, 2, 3, 4, 5, 6];
        var scratch = new ScratchSpans(buffer, spanLength: 2);

        scratch.Count.ShouldBe(3);
        scratch[0].ToArray().ShouldBe([1.0, 2.0]);
        scratch[1].ToArray().ShouldBe([3.0, 4.0]);
        scratch[2].ToArray().ShouldBe([5.0, 6.0]);
    }

    // An operation asking for more spans than it was given used to read past its allowance silently, because the
    // caller passed a single span whatever the declared count was.
    [Fact]
    public void AskingBeyondTheDeclaredCountThrows()
    {
        Span<double> buffer = [1, 2];
        var scratch = new ScratchSpans(buffer, spanLength: 2);

        scratch.Count.ShouldBe(1);
        try
        {
            _ = scratch[1];
            throw new InvalidOperationException("Expected an out-of-range failure.");
        }
        catch (ArgumentOutOfRangeException)
        {
        }
    }
}
