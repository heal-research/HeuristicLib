using HEAL.HeuristicLib.Problems.TravelingSalesman;

namespace HEAL.HeuristicLib.Tests.Problems;

public class TravelingSalesmanCoordinatesDataTests
{
  [Fact]
  public void TupleConstructor_ShouldSetNumberOfCities()
  {
    var data = new TravelingSalesmanCoordinatesData([
      (0.0, 0.0),
      (3.0, 4.0)
    ]);

    Assert.Equal(2, data.NumberOfCities);
  }

  [Fact]
  public void MatrixConstructor_ShouldSetNumberOfCities()
  {
    var coordinates = new double[,] {
      { 0, 0 },
      { 3, 4 },
      { 6, 8 }
    };

    var data = new TravelingSalesmanCoordinatesData(coordinates);

    Assert.Equal(3, data.NumberOfCities);
  }

  [Fact]
  public void TupleConstructor_ShouldThrow_WhenEmpty()
  {
    var ex = Assert.Throws<ArgumentException>(() =>
      new TravelingSalesmanCoordinatesData([]));

    Assert.Contains("at least one city", ex.Message);
  }

  [Fact]
  public void MatrixConstructor_ShouldThrow_WhenColumnCountIsNotTwo()
  {
    var coordinates = new double[,] {
      { 1, 2, 3 }
    };

    var ex = Assert.Throws<ArgumentException>(() =>
      new TravelingSalesmanCoordinatesData(coordinates));

    Assert.Contains("two columns", ex.Message);
  }

  [Fact]
  public void MatrixConstructor_ShouldThrow_WhenThereAreNoCities()
  {
    var coordinates = new double[0, 2];

    var ex = Assert.Throws<ArgumentException>(() =>
      new TravelingSalesmanCoordinatesData(coordinates));

    Assert.Contains("at least one city", ex.Message);
  }

  [Fact]
  public void GetDistance_ShouldUseConfiguredDistanceMeasure()
  {
    var data = new TravelingSalesmanCoordinatesData(
      [(0.0, 0.0), (1.0, 1.0)],
      DistanceMeasure.UpperEuclidean);

    var result = data.GetDistance(0, 1);

    Assert.Equal(2.0, result);
  }

  [Fact]
  public void TupleConstructor_ShouldCloneCoordinates()
  {
    var coordinates = new[] {
      (0.0, 0.0),
      (3.0, 4.0)
    };

    var data = new TravelingSalesmanCoordinatesData(coordinates);

    coordinates[1] = (100.0, 100.0);

    Assert.Equal(5.0, data.GetDistance(0, 1), 10);
  }

  [Fact]
  public void MatrixConstructor_ShouldCloneCoordinates()
  {
    var coordinates = new double[,] {
      { 0, 0 },
      { 3, 4 }
    };

    var data = new TravelingSalesmanCoordinatesData(coordinates);
    coordinates[1, 0] = 100;
    coordinates[1, 1] = 100;

    Assert.Equal(5.0, data.GetDistance(0, 1), 10);
  }
}
