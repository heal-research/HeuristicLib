using HEAL.HeuristicLib.Encodings;

namespace HEAL.HeuristicLib.Problems;


public class RealVectorTestFunctionProblem : ProblemBase<RealVector>
{
  public enum FunctionType
  {
    Rastrigin,
    Sphere
  }

  private readonly FunctionType functionType;
  private readonly double min;
  private readonly double max;

  public RealVectorTestFunctionProblem(FunctionType functionType, double min, double max)
  {
    this.functionType = functionType;
    this.min = min;
    this.max = max;
  }

  public override double Evaluate(RealVector solution)
  {
    return functionType switch
    {
      FunctionType.Rastrigin => EvaluateRastrigin(solution),
      FunctionType.Sphere => EvaluateSphere(solution),
      _ => throw new NotImplementedException()
    };
  }

  private static double EvaluateRastrigin(RealVector solution) {
    int n = solution.Count;
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

  public RealVectorEncoding CreateRealVectorEncoding()
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
