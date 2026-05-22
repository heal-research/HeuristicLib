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

        data.Size.ShouldBe(2);
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

        data.GetFlow(1, 0).ShouldBe(2);
        data.GetFlow(1, 1).ShouldBe(3);
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

        data.GetDistance(0, 1).ShouldBe(4);
        data.GetDistance(1, 0).ShouldBe(5);
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

        var ex = Should.Throw<ArgumentException>(() => new QuadraticAssignmentProblemData(flows, distances));

        ex.ParamName.ShouldBe("flows");
        ex.Message.ShouldContain("must be square");
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

        var ex = Should.Throw<ArgumentException>(() => new QuadraticAssignmentProblemData(flows, distances));

        ex.ParamName.ShouldBe("distances");
        ex.Message.ShouldContain("must be square");
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

        var ex = Should.Throw<ArgumentException>(() => new QuadraticAssignmentProblemData(flows, distances));

        ex.Message.ShouldContain("same size");
    }
}
