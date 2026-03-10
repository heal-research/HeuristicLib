using HEAL.HeuristicLib.Problems.QuadraticAssignment;

namespace HEAL.HeuristicLib.Tests.Problems;

public class QuadraticAssignmentProblemDataTests
{
  [Fact]
  public void Constructor_ShouldSetSize_FromFlowsMatrix()
  {
    var flows = new double[,] {
      { 0, 1 },
      { 2, 0 }
    };
    var distances = new double[,] {
      { 0, 3 },
      { 4, 0 }
    };

    var data = new QuadraticAssignmentProblemData(flows, distances);

    Assert.Equal(2, data.Size);
  }

  [Fact]
  public void GetFlow_ShouldReturnMatrixEntry()
  {
    var flows = new double[,] {
      { 0, 1 },
      { 2, 3 }
    };
    var distances = new double[,] {
      { 0, 4 },
      { 5, 6 }
    };

    var data = new QuadraticAssignmentProblemData(flows, distances);

    Assert.Equal(2, data.GetFlow(1, 0));
    Assert.Equal(3, data.GetFlow(1, 1));
  }

  [Fact]
  public void GetDistance_ShouldReturnMatrixEntry()
  {
    var flows = new double[,] {
      { 0, 1 },
      { 2, 3 }
    };
    var distances = new double[,] {
      { 0, 4 },
      { 5, 6 }
    };

    var data = new QuadraticAssignmentProblemData(flows, distances);

    Assert.Equal(4, data.GetDistance(0, 1));
    Assert.Equal(5, data.GetDistance(1, 0));
  }

  [Fact]
  public void Constructor_ShouldThrow_WhenFlowsIsNotSquare()
  {
    var flows = new double[,] {
      { 1, 2, 3 },
      { 4, 5, 6 }
    };
    var distances = new double[,] {
      { 0, 1 },
      { 2, 3 }
    };

    var ex = Assert.Throws<ArgumentException>(() => new QuadraticAssignmentProblemData(flows, distances));

    Assert.Equal("flows", ex.ParamName);
    Assert.Contains("must be square", ex.Message);
  }

  [Fact]
  public void Constructor_ShouldThrow_WhenDistancesIsNotSquare()
  {
    var flows = new double[,] {
      { 0, 1 },
      { 2, 3 }
    };
    var distances = new double[,] {
      { 1, 2, 3 },
      { 4, 5, 6 }
    };

    var ex = Assert.Throws<ArgumentException>(() => new QuadraticAssignmentProblemData(flows, distances));

    Assert.Equal("distances", ex.ParamName);
    Assert.Contains("must be square", ex.Message);
  }

  [Fact]
  public void Constructor_ShouldThrow_WhenFlowsAndDistancesHaveDifferentSizes()
  {
    var flows = new double[,] {
      { 0, 1 },
      { 2, 3 }
    };
    var distances = new double[,] {
      { 0, 1, 2 },
      { 3, 4, 5 },
      { 6, 7, 8 }
    };

    var ex = Assert.Throws<ArgumentException>(() => new QuadraticAssignmentProblemData(flows, distances));

    Assert.Contains("same size", ex.Message);
  }
}
