using HEAL.HeuristicLib.Encodings;

namespace HEAL.HeuristicLib.Problems;

public class TravelingSalesmanProblem : ProblemBase<Permutation>
{
  private readonly double[,] distances;
  private readonly int numberOfCities;

  public TravelingSalesmanProblem(double[,] distances)
  {
    this.distances = distances;
    numberOfCities = distances.GetLength(0);
  }

  public override double Evaluate(Permutation solution)
  {
    double totalDistance = 0.0;
    for (int i = 0; i < solution.Count - 1; i++)
    {
      totalDistance += distances[solution[i], solution[i + 1]];
    }
    totalDistance += distances[solution[^1], solution[0]]; // Return to the starting city
    return totalDistance;
  }

  public PermutationEncoding CreatePermutationEncoding()
  {
    var parameters = new PermutationEncodingParameters(numberOfCities);
    return new PermutationEncoding(
    parameters,
    new PermutationCreator(parameters),
    new SwapMutation(parameters),
    new OrderCrossover(parameters)
    );
  }
}
