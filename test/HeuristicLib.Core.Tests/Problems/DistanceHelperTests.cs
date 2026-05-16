using HEAL.HeuristicLib.Problems.TravelingSalesman;

namespace HEAL.HeuristicLib.Tests.Problems;

public class DistanceHelperTests
{
  [Fact]
  public void GetDistance_Euclidean_ShouldReturnExpectedValue()
  {
    var result = DistanceHelper.GetDistance(DistanceMeasure.Euclidean, 0, 0, 3, 4);

    result.ShouldBe(5.0, 1e-10);
  }

  [Fact]
  public void GetDistance_RoundedEuclidean_ShouldReturnRoundedValue()
  {
    var result = DistanceHelper.GetDistance(DistanceMeasure.RoundedEuclidean, 0, 0, 1, 1);

    result.ShouldBe(1.0);
  }

  [Fact]
  public void GetDistance_UpperEuclidean_ShouldReturnCeilingValue()
  {
    var result = DistanceHelper.GetDistance(DistanceMeasure.UpperEuclidean, 0, 0, 1, 1);

    result.ShouldBe(2.0);
  }

  [Fact]
  public void GetDistance_Manhattan_ShouldReturnExpectedValue()
  {
    var result = DistanceHelper.GetDistance(DistanceMeasure.Manhattan, 1, 2, 4, 6);

    result.ShouldBe(7.0);
  }

  [Fact]
  public void GetDistance_Maximum_ShouldReturnExpectedValue()
  {
    var result = DistanceHelper.GetDistance(DistanceMeasure.Maximum, 1, 2, 4, 8);

    result.ShouldBe(6.0);
  }

  [Fact]
  public void GetDistance_Chebyshev_ShouldReturnExpectedValue()
  {
    var result = DistanceHelper.GetDistance(DistanceMeasure.Chebyshev, 1, 2, 4, 8);

    result.ShouldBe(6.0);
  }

  [Fact]
  public void GetDistance_Att_ShouldReturnExpectedValue()
  {
    var result = DistanceHelper.GetDistance(DistanceMeasure.Att, 0, 0, 3, 4);

    result.ShouldBe(2.0);
  }

  [Fact]
  public void GetDistance_Direct_ShouldThrowArgumentException()
  {
    var ex = Should.Throw<ArgumentException>(() =>
      DistanceHelper.GetDistance(DistanceMeasure.Direct, 0, 0, 1, 1));

    ex.Message.ShouldContain("requires distance matrix");
  }

  [Fact]
  public void GetDistance_Geo_ShouldReturnOne_ForEqualCoordinates()
  {
    var result = DistanceHelper.GetDistance(DistanceMeasure.Geo, 10, 20, 10, 20);

    result.ShouldBe(1.0);
  }

  [Fact]
  public void GetDistance_Geo_ShouldBeSymmetric()
  {
    var d1 = DistanceHelper.GetDistance(DistanceMeasure.Geo, 10.0, 20.0, 30.0, 40.0);
    var d2 = DistanceHelper.GetDistance(DistanceMeasure.Geo, 30.0, 40.0, 10.0, 20.0);

    d2.ShouldBe(d1);
  }

  [Fact]
  public void GetDistanceMatrix_ShouldReturnProvidedDistancesInstance_WhenDistancesIsNotNull()
  {
    var distances = new double[,] {
      { 0, 1 },
      { 1, 0 }
    };

    var result = DistanceHelper.GetDistanceMatrix(
      DistanceMeasure.Direct,
      coordinates: null,
      distances: distances,
      dimension: 2);

    result.ShouldBeSameAs(distances);
  }

  [Fact]
  public void GetDistanceMatrix_ShouldThrow_WhenDirectMeasureHasNoDistanceMatrix()
  {
    var ex = Should.Throw<ArgumentException>(() =>
      DistanceHelper.GetDistanceMatrix(
        DistanceMeasure.Direct,
        coordinates: null,
        distances: null,
        dimension: 2));

    ex.ParamName.ShouldBe("distances");
    ex.Message.ShouldContain("requires a distance matrix");
  }

  [Fact]
  public void GetDistanceMatrix_ShouldThrow_WhenCoordinatesAndDistancesAreMissing()
  {
    var ex = Should.Throw<ArgumentNullException>(() =>
      DistanceHelper.GetDistanceMatrix(
        DistanceMeasure.Euclidean,
        coordinates: null,
        distances: null,
        dimension: 2));

    ex.ParamName.ShouldBe("coordinates");
    ex.Message.ShouldContain("Neither distances nor coordinates are provided");
  }

  [Fact]
  public void GetDistanceMatrix_ShouldBuildSymmetricEuclideanMatrix_FromCoordinates()
  {
    var coordinates = new double[,] {
      { 0, 0 },
      { 3, 4 },
      { 6, 8 }
    };

    var result = DistanceHelper.GetDistanceMatrix(
      DistanceMeasure.Euclidean,
      coordinates: coordinates,
      distances: null,
      dimension: 3);

    result[0, 0].ShouldBe(0.0);
    result[1, 1].ShouldBe(0.0);
    result[2, 2].ShouldBe(0.0);

    result[0, 1].ShouldBe(5.0, 1e-10);
    result[1, 0].ShouldBe(5.0, 1e-10);

    result[0, 2].ShouldBe(10.0, 1e-10);
    result[2, 0].ShouldBe(10.0, 1e-10);

    result[1, 2].ShouldBe(5.0, 1e-10);
    result[2, 1].ShouldBe(5.0, 1e-10);
  }

  [Fact]
  public void GetDistanceMatrix_ShouldUseRequestedMeasure()
  {
    var coordinates = new double[,] {
      { 0, 0 },
      { 1, 1 }
    };

    var result = DistanceHelper.GetDistanceMatrix(
      DistanceMeasure.UpperEuclidean,
      coordinates: coordinates,
      distances: null,
      dimension: 2);

    result[0, 1].ShouldBe(2.0);
    result[1, 0].ShouldBe(2.0);
  }
}
