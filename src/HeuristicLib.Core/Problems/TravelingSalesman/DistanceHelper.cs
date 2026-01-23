#pragma warning disable S2368
namespace HEAL.HeuristicLib.Problems.TravelingSalesman;

public static class DistanceHelper
{
  public static double[,] GetDistanceMatrix(DistanceMeasure distanceMeasure, double[,]? coordinates, double[,]? distances, int dimension)
  {
    if (distances != null) {
      return distances;
    }

    distances = new double[dimension, dimension];
    for (var i = 0; i < dimension - 1; i++) {
      for (var j = i + 1; j < dimension; j++) {
        distances[i, j] = GetDistance(i, j, distanceMeasure, coordinates, distances);
        distances[j, i] = distances[i, j];
      }
    }

    return distances;
  }

  public static double GetDistance(DistanceMeasure distanceMeasure, double x1, double y1, double x2, double y2)
  {
    return distanceMeasure switch {
      DistanceMeasure.Att => AttDistance(x1, y1, x2, y2),
      DistanceMeasure.Direct => throw new ArgumentException("Direct distance measure requires distance matrix for distance calculation."),
      DistanceMeasure.Euclidean => EuclideanDistance(x1, y1, x2, y2),
      DistanceMeasure.Geo => GeoDistance(x1, y1, x2, y2),
      DistanceMeasure.Manhattan => ManhattanDistance(x1, y1, x2, y2),
      DistanceMeasure.Maximum => MaximumDistance(x1, y1, x2, y2),
      DistanceMeasure.RoundedEuclidean => Math.Round(EuclideanDistance(x1, y1, x2, y2)),
      DistanceMeasure.UpperEuclidean => Math.Ceiling(EuclideanDistance(x1, y1, x2, y2)),
      DistanceMeasure.Chebyshev => ChebyshevDistance(x1, y1, x2, y2),
      _ => throw new InvalidOperationException("Distance measure is not known.")
    };
  }

  private static double ChebyshevDistance(double x1, double y1, double x2, double y2) => Math.Max(Math.Abs(x1 - x2), Math.Abs(y1 - y2));

  #region Private Helpers

  private static double GetDistance(int i, int j, DistanceMeasure distanceMeasure, double[,]? coordinates, double[,]? distances)
  {
    return distanceMeasure switch {
      DistanceMeasure.Att => AttDistance(coordinates![i, 0], coordinates[i, 1], coordinates[j, 0], coordinates[j, 1]),
      DistanceMeasure.Direct => distances![i, j],
      DistanceMeasure.Euclidean => EuclideanDistance(coordinates![i, 0], coordinates[i, 1], coordinates[j, 0], coordinates[j, 1]),
      DistanceMeasure.Geo => GeoDistance(coordinates![i, 0], coordinates[i, 1], coordinates[j, 0], coordinates[j, 1]),
      DistanceMeasure.Manhattan => ManhattanDistance(coordinates![i, 0], coordinates[i, 1], coordinates[j, 0], coordinates[j, 1]),
      DistanceMeasure.Maximum => MaximumDistance(coordinates![i, 0], coordinates[i, 1], coordinates[j, 0], coordinates[j, 1]),
      DistanceMeasure.RoundedEuclidean => Math.Round(EuclideanDistance(coordinates![i, 0], coordinates[i, 1], coordinates[j, 0], coordinates[j, 1])),
      DistanceMeasure.UpperEuclidean => Math.Ceiling(EuclideanDistance(coordinates![i, 0], coordinates[i, 1], coordinates[j, 0], coordinates[j, 1])),
      DistanceMeasure.Chebyshev => ChebyshevDistance(coordinates![i, 0], coordinates[i, 1], coordinates[j, 0], coordinates[j, 1]),
      _ => throw new InvalidOperationException("Distance measure is not known.")
    };
  }

  private static double AttDistance(double x1, double y1, double x2, double y2) => Math.Ceiling(Math.Sqrt((((x1 - x2) * (x1 - x2)) + ((y1 - y2) * (y1 - y2))) / 10.0));

  private static double EuclideanDistance(double x1, double y1, double x2, double y2) => Math.Sqrt(((x1 - x2) * (x1 - x2)) + ((y1 - y2) * (y1 - y2)));

  private const double Pi = 3.141592;
  private const double Radius = 6378.388;

  private static double GeoDistance(double x1, double y1, double x2, double y2)
  {
    var latitude1 = ConvertToRadian(x1);
    var longitude1 = ConvertToRadian(y1);
    var latitude2 = ConvertToRadian(x2);
    var longitude2 = ConvertToRadian(y2);

    var q1 = Math.Cos(longitude1 - longitude2);
    var q2 = Math.Cos(latitude1 - latitude2);
    var q3 = Math.Cos(latitude1 + latitude2);

    return (int)((Radius * Math.Acos(0.5 * (((1.0 + q1) * q2) - ((1.0 - q1) * q3)))) + 1.0);
  }

  private static double ConvertToRadian(double x) => Pi * (Math.Truncate(x) + (5.0 * (x - Math.Truncate(x)) / 3.0)) / 180.0;

  private static double ManhattanDistance(double x1, double y1, double x2, double y2) => Math.Round(Math.Abs(x1 - x2) + Math.Abs(y1 - y2), MidpointRounding.AwayFromZero);

  private static double MaximumDistance(double x1, double y1, double x2, double y2) => Math.Max(Math.Round(Math.Abs(x1 - x2), MidpointRounding.AwayFromZero), Math.Round(Math.Abs(y1 - y2), MidpointRounding.AwayFromZero));

  #endregion
}
