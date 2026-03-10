#pragma warning disable S2368

namespace HEAL.HeuristicLib.Problems.TravelingSalesman;

public class TravelingSalesmanDistanceMatrixProblemData : ITravelingSalesmanProblemData
{
  private readonly double[,] distances;

  public TravelingSalesmanDistanceMatrixProblemData(double[,] distances)
  {
    if (distances.GetLength(0) != distances.GetLength(1))
      throw new ArgumentException("The distance matrix must be square.");

    if (distances.GetLength(0) < 1)
      throw new ArgumentException("The distance matrix must have at least one city.");
    this.distances = (double[,])distances.Clone(); // clone Distances to prevent modification
  }

  public ReadOnlyMatrixView<double> Distances => new(distances);

  public int NumberOfCities => distances.GetLength(0);

  public double GetDistance(int fromCity, int toCity) => distances[fromCity, toCity];
}
