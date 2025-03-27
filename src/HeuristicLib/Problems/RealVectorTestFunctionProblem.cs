using HEAL.HeuristicLib.Core;
using HEAL.HeuristicLib.Encodings;

namespace HEAL.HeuristicLib.Problems;

public class RealVectorTestFunctionProblem : ProblemBase<RealVector> {
  public enum FunctionType {
    Rastrigin,
    Sphere
  }

  private readonly FunctionType functionType;
  private readonly double min;
  private readonly double max;

  public RealVectorTestFunctionProblem(FunctionType functionType, double min, double max) 
    : base(SingleObjective.Minimize) 
  {
    this.functionType = functionType;
    this.min = min;
    this.max = max;
  }

  public override Fitness Evaluate(RealVector solution) {
    double fitness = functionType switch {
      FunctionType.Rastrigin => EvaluateRastrigin(solution),
      FunctionType.Sphere => EvaluateSphere(solution),
      _ => throw new NotImplementedException()
    };
    return fitness;
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

  public RealVectorEncodingParameter CreateRealVectorEncodingParameter() {
    return new RealVectorEncodingParameter(length: 2, min, max);
  }
  public RealVectorEncoding<RealVector> CreateRealVectorEncoding() {
    var parameter = CreateRealVectorEncodingParameter();

    return new RealVectorEncoding(parameter) {
      Creator = new UniformDistributedCreator(minimum: null, maximum: null), 
      Crossover = new AlphaBetaBlendCrossover(alpha: 0.7, beta: 0.3),
      Mutator = new GaussianMutator(mutationRate: 0.1, mutationStrength: 0.1)
    };
  }
  
  // public GeneticAlgorithmSpec CreateGeneticAlgorithmDefaultConfig() {
  //   return new GeneticAlgorithmSpec(
  //     Creator: functionType == FunctionType.Sphere ? new UniformRealVectorCreatorSpec() : new NormalRealVectorCreatorSpec([5], [0.5]),
  //     Crossover: new AlphaBetaBlendRealVectorCrossoverSpec(Alpha: 0.8, Beta: 0.2),
  //     Mutator: new GaussianRealVectorMutatorSpec()
  //   );
  // }
}

// public static class GeneticAlgorithmBuilderRealVectorTestFunctionExtensions {
//   public static GeneticAlgorithmBuilder<RealVectorEncoding, RealVector> WithProblemEncoding
//     (this GeneticAlgorithmBuilder<RealVectorEncoding, RealVector> builder, RealVectorTestFunctionProblem problem)
//   {
//     builder.WithEvaluator(problem.CreateEvaluator());
//     builder.WithEncoding(problem.CreateRealVectorEncodingEncoding());
//     builder.WithSpecs(problem.CreateGeneticAlgorithmDefaultConfig());
//     return builder;
//     }
// }
