namespace HEAL.HeuristicLib.Problems.TravelingSalesman.InstanceLoading;

public class TspData
{
  /// <summary>
  ///   The name of the instance
  /// </summary>
  public string Name { get; set; } = string.Empty;

  /// <summary>
  ///   Optional! The description of the instance
  /// </summary>
  public string Description { get; set; } = string.Empty;

  /// <summary>
  ///   The number of cities.
  /// </summary>
  public int Dimension { get; set; }

  /// <summary>
  ///   Specifies the distance measure that is to be used.
  /// </summary>
  public DistanceMeasure DistanceMeasure { get; set; }

  /// <summary>
  ///   Optional! The Distances are given in form of a distance matrix.
  /// </summary>
  /// <remarks>
  ///   Either this property or <see cref="Coordinates" /> needs to be specified.
  /// </remarks>
  public double[,]? Distances { get; set; }

  /// <summary>
  ///   Optional! A matrix of dimension [N, 2] where each row is one of the cities
  ///   and the columns represent x and y coordinates respectively.
  /// </summary>
  /// <remarks>
  ///   Either this property or <see cref="Distances" /> needs to be specified.
  ///   If no distance matrix is given, the Distances have to be calculated by the
  ///   specified distance measure. If a distance matrix is given in addition to the
  ///   coordinates, the distance matrix takes precedence and the coordinates are
  ///   for display only.
  /// </remarks>
  public double[,]? Coordinates { get; set; }

  /// <summary>
  ///   Optional! The best-known tour in path-encoding.
  /// </summary>
  public int[]? BestKnownTour { get; set; }

  /// <summary>
  ///   Optional! The quality of the best-known tour.
  /// </summary>
  public double? BestKnownQuality { get; set; }

  /// <summary>
  ///   If only the coordinates are given, can calculate the distance matrix.
  /// </summary>
  /// <returns>A full distance matrix between all cities.</returns>
  public double[,] GetDistanceMatrix() => DistanceHelper.GetDistanceMatrix(DistanceMeasure, Coordinates, Distances, Dimension);

  public TravelingSalesmanCoordinatesData ToCoordinatesData()
  {
    if (Coordinates == null) {
      throw new InvalidOperationException("Cannot convert to TravelingSalesmanCoordinatesData because no coordinates are given.");
    }

    return new TravelingSalesmanCoordinatesData(Coordinates, DistanceMeasure);
  }

  public TravelingSalesmanDistanceMatrixProblemData ToDistanceMatrixData()
  {
    var distanceMatrix = GetDistanceMatrix();

    return new TravelingSalesmanDistanceMatrixProblemData(distanceMatrix);
  }
}
