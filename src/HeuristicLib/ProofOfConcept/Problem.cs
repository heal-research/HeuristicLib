namespace HEAL.HeuristicLib.ProofOfConcept;

public interface IProblem<TSolution>
{
    double Evaluate(TSolution solution);
    IEncoding<TSolution> CreateEncoding();
}

public class RealVectorTestFunction : IProblem<RealVector>
{
  public enum FunctionType
  {
    Rastrigin,
    Sphere
  }

  private readonly FunctionType functionType;
  private readonly double min;
  private readonly double max;

  public RealVectorTestFunction(FunctionType functionType, double min, double max)
  {
    this.functionType = functionType;
    this.min = min;
    this.max = max;
  }

  public double Evaluate(RealVector solution)
  {
    return functionType switch
    {
      FunctionType.Rastrigin => EvaluateRastrigin(solution),
      FunctionType.Sphere => EvaluateSphere(solution),
      _ => throw new NotImplementedException()
    };
  }

  private static double EvaluateRastrigin(RealVector solution)
  {
    int n = solution.Count();
    double A = 10;
    double sum = A * n;
    for (int i = 0; i < n; i++)
    {
      sum += solution[i] * solution[i] - A * Math.Cos(2 * Math.PI * solution[i]);
    }
    return sum;
  }

  private static double EvaluateSphere(RealVector solution)
  {
    return solution.Sum(x => x * x);
  }

  public IEncoding<RealVector> CreateEncoding()
  {
    var parameters = new RealVectorEncodingParameters(10, min, max); // Assuming length 10 for example
    return new RealVectorEncoding(
      parameters,
      new RealVectorCreator(parameters),
      new GaussianMutation(0.1, 0.1),
      new AlphaBetaBlendCrossover(0.7, 0.3)
    );
  }
}

public class TravelingSalesmanProblem : IProblem<Permutation>
{
  private readonly double[,] distances;
  private readonly int numberOfCities;

  public TravelingSalesmanProblem(double[,] distances)
  {
    this.distances = distances;
    numberOfCities = distances.GetLength(0);
  }

  public double Evaluate(Permutation solution)
  {
    double totalDistance = 0.0;
    for (int i = 0; i < solution.Count() - 1; i++)
    {
      totalDistance += distances[solution[i], solution[i + 1]];
    }
    totalDistance += distances[solution[^1], solution[0]]; // Return to the starting city
    return totalDistance;
  }

  public IEncoding<Permutation> CreateEncoding()
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
