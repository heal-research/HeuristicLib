using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Problems;

public class RealVectorTestFunctionProblem : ProblemBase<RealVector, ObjectiveValue>
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

  public override ObjectiveValue Evaluate(RealVector solution)
  {
    double objective = functionType switch
    {
      FunctionType.Rastrigin => EvaluateRastrigin(solution),
      FunctionType.Sphere => EvaluateSphere(solution),
      _ => throw new NotImplementedException()
    };
    return (objective, ObjectiveDirection.Minimize);
  }

  public override IEvaluator<RealVector, ObjectiveValue> CreateEvaluator()
  {
    return new RealVectorTestFunctionEvaluator(this);
  }

  private class RealVectorTestFunctionEvaluator : IEvaluator<RealVector, ObjectiveValue>
  {
    private readonly RealVectorTestFunctionProblem problem;

    public RealVectorTestFunctionEvaluator(RealVectorTestFunctionProblem problem)
    {
      this.problem = problem;
    }

    public ObjectiveValue Evaluate(RealVector solution)
    {
      return problem.Evaluate(solution);
    }
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

  public TestFunctionRealVectorEncodingBundle CreateRealVectorEncodingBundle()
  {
    var encoding = new RealVectorEncoding(10, min, max); // Assuming length 10 for example

    return new TestFunctionRealVectorEncodingBundle(
      encoding,
      UniformDistributedCreator.FromEncoding(encoding),
      new AlphaBetaBlendCrossover(0.7, 0.3),
      new GaussianMutation(0.1, 0.1)
    );
  }
}

public class TestFunctionRealVectorEncodingBundle : IEncodingBundle<RealVector, RealVectorEncoding>, ICreatorProvider<RealVector>, ICrossoverProvider<RealVector>, IMutatorProvider<RealVector>
{
  public RealVectorEncoding Encoding { get; }
  public ICreator<RealVector> Creator { get; }
  public ICrossover<RealVector> Crossover { get; }
  public IMutator<RealVector> Mutator { get; }

  public TestFunctionRealVectorEncodingBundle(RealVectorEncoding encoding, ICreator<RealVector> creator, ICrossover<RealVector> crossover, IMutator<RealVector> mutator)
  {
    Encoding = encoding;
    Creator = creator;
    Crossover = crossover;
    Mutator = mutator;
  }
}

