using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Problems;

public record Tour(IReadOnlyList<int> Cities);

public class TravelingSalesmanProblem 
  //: ProblemBase<Tour>
  : ProblemBase<Permutation>
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
    //var tour = solution.Cities;
    var tour = solution;
    double totalDistance = 0.0;
    for (int i = 0; i < tour.Count - 1; i++)
    {
      totalDistance += distances[tour[i], tour[i + 1]];
    }
    totalDistance += distances[tour[^1], tour[0]]; // Return to the starting city
    return totalDistance;
  }

  public override IEvaluator<Permutation> CreateEvaluator()
  {
    return new TSPEvaluator(this);
  }

  private class TSPEvaluator : IEvaluator<Permutation>
  {
    private readonly TravelingSalesmanProblem problem;

    public TSPEvaluator(TravelingSalesmanProblem problem)
    {
      this.problem = problem;
    }

    public double Evaluate(Permutation solution)
    {
      return problem.Evaluate(solution);
    }
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
