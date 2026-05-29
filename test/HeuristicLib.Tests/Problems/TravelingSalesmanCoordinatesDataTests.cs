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

        data.NumberOfCities.ShouldBe(2);
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

        data.NumberOfCities.ShouldBe(3);
    }

    [Fact]
    public void TupleConstructor_ShouldThrow_WhenEmpty()
    {
        var ex = Should.Throw<ArgumentException>(() =>
          new TravelingSalesmanCoordinatesData([]));

        ex.Message.ShouldContain("at least one city");
    }

    [Fact]
    public void MatrixConstructor_ShouldThrow_WhenColumnCountIsNotTwo()
    {
        var coordinates = new double[,] {
      { 1, 2, 3 }
    };

        var ex = Should.Throw<ArgumentException>(() =>
          new TravelingSalesmanCoordinatesData(coordinates));

        ex.Message.ShouldContain("two columns");
    }

    [Fact]
    public void MatrixConstructor_ShouldThrow_WhenThereAreNoCities()
    {
        var coordinates = new double[0, 2];

        var ex = Should.Throw<ArgumentException>(() =>
          new TravelingSalesmanCoordinatesData(coordinates));

        ex.Message.ShouldContain("at least one city");
    }

    [Fact]
    public void GetDistance_ShouldUseConfiguredDistanceMeasure()
    {
        var data = new TravelingSalesmanCoordinatesData(
          [(0.0, 0.0), (1.0, 1.0)],
          DistanceMeasure.UpperEuclidean);

        var result = data.GetDistance(0, 1);

        result.ShouldBe(2.0);
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

        data.GetDistance(0, 1).ShouldBe(5.0, 1e-10);
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

        data.GetDistance(0, 1).ShouldBe(5.0, 1e-10);
    }
}
