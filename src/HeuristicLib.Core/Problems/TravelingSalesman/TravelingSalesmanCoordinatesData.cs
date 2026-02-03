#pragma warning disable S2368
namespace HEAL.HeuristicLib.Problems.TravelingSalesman;

public class TravelingSalesmanCoordinatesData : ITravelingSalesmanProblemData {
  public int NumberOfCities => Coordinates.Count;
  public IReadOnlyList<(double X, double Y)> Coordinates { get; }
  public DistanceMeasure DistanceMeasure { get; }

  public TravelingSalesmanCoordinatesData((double X, double Y)[] coordinates, DistanceMeasure measure = DistanceMeasure.Euclidean) {
    if (coordinates.Length < 1) throw new ArgumentException("The coordinates must have at least one city.");
    Coordinates = coordinates.ToArray(); // clone coordinates to prevent modification
    DistanceMeasure = measure;
  }

  public TravelingSalesmanCoordinatesData(double[,] coordinates, DistanceMeasure measure = DistanceMeasure.Euclidean) {
    if (coordinates.GetLength(1) != 2) throw new ArgumentException("The coordinates must have two columns.");
    if (coordinates.GetLength(0) < 1) throw new ArgumentException("The coordinates must have at least one city.");

    var data = new (double X, double Y)[coordinates.GetLength(0)];
    for (var i = 0; i < coordinates.GetLength(0); i++) {
      data[i] = (coordinates[i, 0], coordinates[i, 1]);
    }

    Coordinates = data;
    DistanceMeasure = measure;
  }

  public double GetDistance(int fromCity, int toCity) {
    (var x1, var y1) = Coordinates[fromCity];
    (var x2, var y2) = Coordinates[toCity];

    return DistanceHelper.GetDistance(DistanceMeasure, x1, y1, x2, y2);
  }
}
