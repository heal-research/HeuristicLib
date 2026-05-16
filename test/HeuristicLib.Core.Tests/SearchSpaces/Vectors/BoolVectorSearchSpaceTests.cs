using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Tests.SearchSpaces.Vectors;

public class BoolVectorSearchSpaceTests
{
  [Fact]
  public void Constructor_SetsLength()
  {
    var space = new BoolVectorSearchSpace(3);

    space.Length.ShouldBe(3);
  }

  [Fact]
  public void Contains_ReturnsTrue_WhenLengthMatches()
  {
    var space = new BoolVectorSearchSpace(3);

    bool[] values = [true, false, true];
    space.Contains(BoolVector.Create(values)).ShouldBeTrue();
  }

  [Fact]
  public void Contains_ReturnsFalse_WhenLengthIsTooShort()
  {
    var space = new BoolVectorSearchSpace(3);

    bool[] values = [true, false];
    space.Contains(BoolVector.Create(values)).ShouldBeFalse();
  }

  [Fact]
  public void Contains_ReturnsFalse_WhenLengthIsTooLong()
  {
    var space = new BoolVectorSearchSpace(3);

    bool[] values = [true, false, true, false];
    space.Contains(BoolVector.Create(values)).ShouldBeFalse();
  }

  [Fact]
  public void Contains_ReturnsTrue_ForEmptyVector_WhenLengthIsZero()
  {
    var space = new BoolVectorSearchSpace(0);

    space.Contains(BoolVector.Create(Array.Empty<bool>())).ShouldBeTrue();
  }

  [Fact]
  public void Contains_IsIndependentOfActualBooleanValues()
  {
    var space = new BoolVectorSearchSpace(3);

    bool[] values = [true, true, true];
    space.Contains(BoolVector.Create(values)).ShouldBeTrue();
    bool[] values1 = [false, false, false];
    space.Contains(BoolVector.Create(values1)).ShouldBeTrue();
    bool[] values2 = [true, false, true];
    space.Contains(BoolVector.Create(values2)).ShouldBeTrue();
  }
}
