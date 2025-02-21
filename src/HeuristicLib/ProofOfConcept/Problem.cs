namespace HEAL.HeuristicLib.ProofOfConcept;

public interface IProblem<in TSolution>
{
  double Evaluate(TSolution solution);
}

public class RealVectorTestFunction : IProblem<double[]>
{
  public enum FunctionType
  {
    Rastrigin,
    Sphere
  }

  private readonly FunctionType functionType;

  public RealVectorTestFunction(FunctionType functionType)
  {
    this.functionType = functionType;
  }

  public double Evaluate(double[] solution)
  {
    return functionType switch
    {
      FunctionType.Rastrigin => EvaluateRastrigin(solution),
      FunctionType.Sphere => EvaluateSphere(solution),
      _ => throw new ArgumentOutOfRangeException()
    };
  }

  private double EvaluateRastrigin(double[] solution)
  {
    int n = solution.Length;
    double A = 10;
    double sum = A * n;
    for (int i = 0; i < n; i++)
    {
      sum += solution[i] * solution[i] - A * Math.Cos(2 * Math.PI * solution[i]);
    }
    return sum;
  }

  private double EvaluateSphere(double[] solution)
  {
    return solution.Sum(x => x * x);
  }
}

public class TravelingSalesmanProblem : IProblem<int[]>
{
  private readonly double[,] distances;

  public TravelingSalesmanProblem(double[,] distances)
  {
    this.distances = distances;
  }

  public double Evaluate(int[] solution)
  {
    double totalDistance = 0.0;
    for (int i = 0; i < solution.Length - 1; i++)
    {
      totalDistance += distances[solution[i], solution[i + 1]];
    }
    totalDistance += distances[solution[^1], solution[0]]; // Return to the starting city
    return totalDistance;
  }
}
