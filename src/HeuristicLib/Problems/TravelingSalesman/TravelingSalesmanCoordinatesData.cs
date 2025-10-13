namespace HEAL.HeuristicLib.Problems.TravelingSalesman;

public class TravelingSalesmanCoordinatesData : ITravelingSalesmanProblemData {
  public int NumberOfCities => Coordinates.Count;
  public IReadOnlyList<(double X, double Y)> Coordinates { get; }
  public DistanceMetric DistanceMetric { get; }

  public TravelingSalesmanCoordinatesData((double X, double Y)[] coordinates, DistanceMetric metric = DistanceMetric.Euclidean) {
    if (coordinates.Length < 1) throw new ArgumentException("The coordinates must have at least one city.");

    Coordinates = coordinates.ToArray(); // clone coordinates to prevent modification
    DistanceMetric = metric;
  }

#pragma warning disable S2368
  public TravelingSalesmanCoordinatesData(double[,] coordinates, DistanceMetric metric = DistanceMetric.Euclidean) {
    if (coordinates.GetLength(1) != 2) throw new ArgumentException("The coordinates must have two columns.");
    if (coordinates.GetLength(0) < 1) throw new ArgumentException("The coordinates must have at least one city.");

    var data = new (double X, double Y)[coordinates.GetLength(0)];
    for (int i = 0; i < coordinates.GetLength(0); i++) {
      data[i] = (coordinates[i, 0], coordinates[i, 1]);
    }

    Coordinates = data;
    DistanceMetric = metric;
  }
#pragma warning restore S2368

  public double GetDistance(int fromCity, int toCity) {
    (double x1, double y1) = Coordinates[fromCity];
    (double x2, double y2) = Coordinates[toCity];

    return DistanceMetric switch {
      DistanceMetric.Unknown => throw new ArgumentException("The distance metric is unknown."),
      DistanceMetric.Euclidean => Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2)),
      DistanceMetric.Manhattan => Math.Abs(x1 - x2) + Math.Abs(y1 - y2),
      DistanceMetric.Chebyshev => Math.Max(Math.Abs(x1 - x2), Math.Abs(y1 - y2)),
      _ => throw new NotImplementedException()
    };
  }
}
