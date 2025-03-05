using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;
using HEAL.HeuristicLib.Configuration;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Problems;

public class RealVectorTestFunctionProblem : ProblemBase<RealVector, ObjectiveValue> {
  public enum FunctionType {
    Rastrigin,
    Sphere
  }

  private readonly FunctionType functionType;
  private readonly double min;
  private readonly double max;

  public RealVectorTestFunctionProblem(FunctionType functionType, double min, double max) {
    this.functionType = functionType;
    this.min = min;
    this.max = max;
  }

  public override ObjectiveValue Evaluate(RealVector solution) {
    double objective = functionType switch {
      FunctionType.Rastrigin => EvaluateRastrigin(solution),
      FunctionType.Sphere => EvaluateSphere(solution),
      _ => throw new NotImplementedException()
    };
    return (objective, ObjectiveDirection.Minimize);
  }

  public override IEvaluator<RealVector, ObjectiveValue> CreateEvaluator() {
    return new RealVectorTestFunctionEvaluator(this);
  }

  private class RealVectorTestFunctionEvaluator : EvaluatorBase<RealVector, ObjectiveValue>  {
    private readonly RealVectorTestFunctionProblem problem;

    public RealVectorTestFunctionEvaluator(RealVectorTestFunctionProblem problem) {
      this.problem = problem;
    }

    public override ObjectiveValue Evaluate(RealVector solution) {
      return problem.Evaluate(solution);
    }
  }

  private static double EvaluateRastrigin(RealVector solution) {
    int n = solution.Count;
    double A = 10;
    double sum = A * n;
    for (int i = 0; i < n; i++) {
      sum += solution[i] * solution[i] - A * Math.Cos(2 * Math.PI * solution[i]);
    }
    return sum;
  }

  private static double EvaluateSphere(RealVector solution) {
    return solution.Sum(x => x * x);
  }

  public RealVectorEncoding CreateRealVectorEncodingEncoding() {
    return new RealVectorEncoding(length: 2, min, max);
  }
  
  public GeneticAlgorithmSpec CreateGeneticAlgorithmDefaultConfig() {
    return new GeneticAlgorithmSpec(
      Creator: functionType == FunctionType.Sphere ? new UniformRealVectorCreatorSpec() : new NormalRealVectorCreatorSpec([5], [0.5]),
      Crossover: new AlphaBlendRealVectorCrossoverSpec(Alpha: 0.8, Beta: 0.2),
      Mutator: new GaussianRealVectorMutatorSpec()
    );
  }
}

public static class GeneticAlgorithmBuilderRealVectorTestFunctionExtensions {
  public static GeneticAlgorithmBuilder<RealVectorEncoding, RealVector> WithProblemEncoding
    (this GeneticAlgorithmBuilder<RealVectorEncoding, RealVector> builder, RealVectorTestFunctionProblem problem)
  {
    builder.WithEvaluator(problem.CreateEvaluator());
    builder.WithEncoding(problem.CreateRealVectorEncodingEncoding());
    builder.WithSpecs(problem.CreateGeneticAlgorithmDefaultConfig());
    return builder;
    }
}
