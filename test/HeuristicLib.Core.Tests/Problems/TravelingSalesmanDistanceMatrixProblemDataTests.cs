using HEAL.HeuristicLib.Problems.TravelingSalesman;

namespace HEAL.HeuristicLib.Tests.Problems;

public class TravelingSalesmanDistanceMatrixProblemDataTests
{
    [Fact]
    public void Constructor_ShouldSetNumberOfCities()
    {
        var distances = new double[,] {
      { 0, 1 },
      { 1, 0 }
    };

        var data = new TravelingSalesmanDistanceMatrixProblemData(distances);

        data.NumberOfCities.ShouldBe(2);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenMatrixIsNotSquare()
    {
        var distances = new double[,] {
      { 0, 1, 2 },
      { 3, 4, 5 }
    };

        var ex = Should.Throw<ArgumentException>(() =>
          new TravelingSalesmanDistanceMatrixProblemData(distances));

        ex.Message.ShouldContain("must be square");
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenThereAreNoCities()
    {
        var distances = new double[0, 0];

        var ex = Should.Throw<ArgumentException>(() =>
          new TravelingSalesmanDistanceMatrixProblemData(distances));

        ex.Message.ShouldContain("at least one city");
    }

    [Fact]
    public void GetDistance_ShouldReturnMatrixEntry()
    {
        var distances = new double[,] {
      { 0, 7 },
      { 9, 0 }
    };

        var data = new TravelingSalesmanDistanceMatrixProblemData(distances);

        data.GetDistance(0, 1).ShouldBe(7.0);
        data.GetDistance(1, 0).ShouldBe(9.0);
    }

    [Fact]
    public void Constructor_ShouldCloneDistanceMatrix()
    {
        var distances = new double[,] {
      { 0, 7 },
      { 9, 0 }
    };

        var data = new TravelingSalesmanDistanceMatrixProblemData(distances);
        distances[0, 1] = 123;

        data.GetDistance(0, 1).ShouldBe(7.0);
    }

    [Fact]
    public void Distances_ShouldReturnClonedView()
    {
        var data = new TravelingSalesmanDistanceMatrixProblemData(new double[,] {
      { 0, 7 },
      { 9, 0 }
    });

        var cloneA = data.Distances;
        var cloneB = data.Distances;

        cloneB.ShouldNotBeSameAs(cloneA);
        cloneA[0, 1].ShouldBe(7.0);
        cloneA[1, 0].ShouldBe(9.0);
    }
}
