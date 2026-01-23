namespace HEAL.HeuristicLib.Problems.TravelingSalesman;

public class TravelingSalesmanDistanceMatrixProblemData : ITravelingSalesmanProblemData
{
  private readonly double[,] distances;

  public int NumberOfCities => distances.GetLength(0);
  public IReadOnlyList<IReadOnlyList<double>> Distances => Clone(distances);

#pragma warning disable S2368
  public TravelingSalesmanDistanceMatrixProblemData(double[,] distances)
  {
    if (distances.GetLength(0) != distances.GetLength(1)) {
      throw new ArgumentException("The distance matrix must be square.");
    }

    if (distances.GetLength(0) < 1) {
      throw new ArgumentException("The distance matrix must have at least one city.");
    }

    this.distances = (double[,])distances.Clone(); // clone distances to prevent modification
  }
#pragma warning restore S2368

  public double GetDistance(int fromCity, int toCity) => distances[fromCity, toCity];

  private static IReadOnlyList<IReadOnlyList<double>> Clone(double[,] array)
  {
    var result = new double[array.GetLength(0)][];
    for (var i = 0; i < array.GetLength(0); i++) {
      result[i] = new double[array.GetLength(1)];
      for (var j = 0; j < array.GetLength(1); j++) {
        result[i][j] = array[i, j];
      }
    }

    return result;
  }
}
