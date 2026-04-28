using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Tests.SearchSpaces.Vectors;

public class BoolVectorSearchSpaceTests
{
  [Fact]
  public void Constructor_SetsLength()
  {
    var space = new BoolVectorSearchSpace(3);

    Assert.Equal(3, space.Length);
  }

  [Fact]
  public void Contains_ReturnsTrue_WhenLengthMatches()
  {
    var space = new BoolVectorSearchSpace(3);

    bool[] values = [true, false, true];
    Assert.True(space.Contains(values));
  }

  [Fact]
  public void Contains_ReturnsFalse_WhenLengthIsTooShort()
  {
    var space = new BoolVectorSearchSpace(3);

    bool[] values = [true, false];
    Assert.False(space.Contains(values));
  }

  [Fact]
  public void Contains_ReturnsFalse_WhenLengthIsTooLong()
  {
    var space = new BoolVectorSearchSpace(3);

    bool[] values = [true, false, true, false];
    Assert.False(space.Contains(values));
  }

  [Fact]
  public void Contains_ReturnsTrue_ForEmptyVector_WhenLengthIsZero()
  {
    var space = new BoolVectorSearchSpace(0);

    Assert.True(space.Contains(Array.Empty<bool>()));
  }

  [Fact]
  public void Contains_IsIndependentOfActualBooleanValues()
  {
    var space = new BoolVectorSearchSpace(3);

    bool[] values = [true, true, true];
    Assert.True(space.Contains(values));
    bool[] values1 = [false, false, false];
    Assert.True(space.Contains(values1));
    bool[] values2 = [true, false, true];
    Assert.True(space.Contains(values2));
  }
}
