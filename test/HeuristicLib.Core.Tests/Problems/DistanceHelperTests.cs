using HEAL.HeuristicLib.Problems.TravelingSalesman;

namespace HEAL.HeuristicLib.Tests.Problems;

public class DistanceHelperTests
{
  [Fact]
  public void GetDistance_Euclidean_ShouldReturnExpectedValue()
  {
    var result = DistanceHelper.GetDistance(DistanceMeasure.Euclidean, 0, 0, 3, 4);

    Assert.Equal(5.0, result, 10);
  }

  [Fact]
  public void GetDistance_RoundedEuclidean_ShouldReturnRoundedValue()
  {
    var result = DistanceHelper.GetDistance(DistanceMeasure.RoundedEuclidean, 0, 0, 1, 1);

    Assert.Equal(1.0, result);
  }

  [Fact]
  public void GetDistance_UpperEuclidean_ShouldReturnCeilingValue()
  {
    var result = DistanceHelper.GetDistance(DistanceMeasure.UpperEuclidean, 0, 0, 1, 1);

    Assert.Equal(2.0, result);
  }

  [Fact]
  public void GetDistance_Manhattan_ShouldReturnExpectedValue()
  {
    var result = DistanceHelper.GetDistance(DistanceMeasure.Manhattan, 1, 2, 4, 6);

    Assert.Equal(7.0, result);
  }

  [Fact]
  public void GetDistance_Maximum_ShouldReturnExpectedValue()
  {
    var result = DistanceHelper.GetDistance(DistanceMeasure.Maximum, 1, 2, 4, 8);

    Assert.Equal(6.0, result);
  }

  [Fact]
  public void GetDistance_Chebyshev_ShouldReturnExpectedValue()
  {
    var result = DistanceHelper.GetDistance(DistanceMeasure.Chebyshev, 1, 2, 4, 8);

    Assert.Equal(6.0, result);
  }

  [Fact]
  public void GetDistance_Att_ShouldReturnExpectedValue()
  {
    var result = DistanceHelper.GetDistance(DistanceMeasure.Att, 0, 0, 3, 4);

    Assert.Equal(2.0, result);
  }

  [Fact]
  public void GetDistance_Direct_ShouldThrowArgumentException()
  {
    var ex = Assert.Throws<ArgumentException>(() =>
      DistanceHelper.GetDistance(DistanceMeasure.Direct, 0, 0, 1, 1));

    Assert.Contains("requires distance matrix", ex.Message);
  }

  [Fact]
  public void GetDistance_Geo_ShouldReturnOne_ForEqualCoordinates()
  {
    var result = DistanceHelper.GetDistance(DistanceMeasure.Geo, 10, 20, 10, 20);

    Assert.Equal(1.0, result);
  }

  [Fact]
  public void GetDistance_Geo_ShouldBeSymmetric()
  {
    var d1 = DistanceHelper.GetDistance(DistanceMeasure.Geo, 10.0, 20.0, 30.0, 40.0);
    var d2 = DistanceHelper.GetDistance(DistanceMeasure.Geo, 30.0, 40.0, 10.0, 20.0);

    Assert.Equal(d1, d2);
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

    Assert.Same(distances, result);
  }

  [Fact]
  public void GetDistanceMatrix_ShouldThrow_WhenDirectMeasureHasNoDistanceMatrix()
  {
    var ex = Assert.Throws<ArgumentException>(() =>
      DistanceHelper.GetDistanceMatrix(
        DistanceMeasure.Direct,
        coordinates: null,
        distances: null,
        dimension: 2));

    Assert.Equal("distances", ex.ParamName);
    Assert.Contains("requires a distance matrix", ex.Message);
  }

  [Fact]
  public void GetDistanceMatrix_ShouldThrow_WhenCoordinatesAndDistancesAreMissing()
  {
    var ex = Assert.Throws<ArgumentNullException>(() =>
      DistanceHelper.GetDistanceMatrix(
        DistanceMeasure.Euclidean,
        coordinates: null,
        distances: null,
        dimension: 2));

    Assert.Equal("coordinates", ex.ParamName);
    Assert.Contains("Neither distances nor coordinates are provided", ex.Message);
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

    Assert.Equal(0.0, result[0, 0]);
    Assert.Equal(0.0, result[1, 1]);
    Assert.Equal(0.0, result[2, 2]);

    Assert.Equal(5.0, result[0, 1], 10);
    Assert.Equal(5.0, result[1, 0], 10);

    Assert.Equal(10.0, result[0, 2], 10);
    Assert.Equal(10.0, result[2, 0], 10);

    Assert.Equal(5.0, result[1, 2], 10);
    Assert.Equal(5.0, result[2, 1], 10);
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

    Assert.Equal(2.0, result[0, 1]);
    Assert.Equal(2.0, result[1, 0]);
  }
}
