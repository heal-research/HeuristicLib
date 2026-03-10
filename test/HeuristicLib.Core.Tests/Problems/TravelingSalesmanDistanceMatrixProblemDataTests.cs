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

    Assert.Equal(2, data.NumberOfCities);
  }

  [Fact]
  public void Constructor_ShouldThrow_WhenMatrixIsNotSquare()
  {
    var distances = new double[,] {
      { 0, 1, 2 },
      { 3, 4, 5 }
    };

    var ex = Assert.Throws<ArgumentException>(() =>
      new TravelingSalesmanDistanceMatrixProblemData(distances));

    Assert.Contains("must be square", ex.Message);
  }

  [Fact]
  public void Constructor_ShouldThrow_WhenThereAreNoCities()
  {
    var distances = new double[0, 0];

    var ex = Assert.Throws<ArgumentException>(() =>
      new TravelingSalesmanDistanceMatrixProblemData(distances));

    Assert.Contains("at least one city", ex.Message);
  }

  [Fact]
  public void GetDistance_ShouldReturnMatrixEntry()
  {
    var distances = new double[,] {
      { 0, 7 },
      { 9, 0 }
    };

    var data = new TravelingSalesmanDistanceMatrixProblemData(distances);

    Assert.Equal(7.0, data.GetDistance(0, 1));
    Assert.Equal(9.0, data.GetDistance(1, 0));
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

    Assert.Equal(7.0, data.GetDistance(0, 1));
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

    Assert.NotSame(cloneA, cloneB);
    Assert.Equal(7.0, cloneA[0, 1]);
    Assert.Equal(9.0, cloneA[1, 0]);
  }
}
