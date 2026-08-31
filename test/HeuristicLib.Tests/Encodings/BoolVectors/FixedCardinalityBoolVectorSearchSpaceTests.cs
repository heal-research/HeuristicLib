using HEAL.HeuristicLib.Encodings.BoolVectors;

namespace HEAL.HeuristicLib.Tests.SearchSpaces.Vectors;

public class FixedCardinalityBoolVectorSearchSpaceTests
{
    [Fact]
    public void Constructor_SetsLengthAndCardinality()
    {
        var space = new FixedCardinalityBoolVectorSearchSpace(length: 5, cardinality: 2);

        space.Length.ShouldBe(5);
        space.Cardinality.ShouldBe(2);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(3, -1)]
    [InlineData(3, 4)]
    public void Constructor_Throws_WhenCardinalityIsOutsideTheLength(int length, int cardinality)
    {
        Should.Throw<ArgumentOutOfRangeException>(
            () => new FixedCardinalityBoolVectorSearchSpace(length, cardinality));
    }

    [Fact]
    public void Contains_ReturnsTrue_WhenLengthAndCardinalityMatch()
    {
        var space = new FixedCardinalityBoolVectorSearchSpace(length: 4, cardinality: 2);

        space.Contains(BoolVector.Create([true, false, true, false])).ShouldBeTrue();
        space.Contains(BoolVector.Create([false, true, false, true])).ShouldBeTrue();
    }

    [Fact]
    public void Contains_ReturnsFalse_WhenCardinalityDiffers()
    {
        var space = new FixedCardinalityBoolVectorSearchSpace(length: 4, cardinality: 2);

        space.Contains(BoolVector.Create([true, true, true, false])).ShouldBeFalse();
        space.Contains(BoolVector.Create([true, false, false, false])).ShouldBeFalse();
    }

    [Fact]
    public void Contains_ReturnsFalse_WhenLengthDiffers()
    {
        var space = new FixedCardinalityBoolVectorSearchSpace(length: 4, cardinality: 2);

        space.Contains(BoolVector.Create([true, true])).ShouldBeFalse();
    }

    /// <summary>
    /// The membership difference this space introduces is the point of it: the same candidate representation belongs
    /// to the unconstrained space and does not belong here, so the search space type argument carries information the
    /// candidate type cannot.
    /// </summary>
    [Fact]
    public void UnconstrainedSpace_AcceptsCandidatesThisSpaceRejects()
    {
        var constrained = new FixedCardinalityBoolVectorSearchSpace(length: 4, cardinality: 2);
        var unconstrained = constrained.ToUnconstrained();

        var offCardinality = BoolVector.Create([true, true, true, false]);

        unconstrained.Length.ShouldBe(constrained.Length);
        unconstrained.Contains(offCardinality).ShouldBeTrue();
        constrained.Contains(offCardinality).ShouldBeFalse();
    }
}
