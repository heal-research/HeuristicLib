using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Configuration;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Problems;

public class RealVectorTestFunctionProblem : ProblemBase<RealVector, RealVector, Fitness, Goal> {
  public enum FunctionType {
    Rastrigin,
    Sphere
  }

  private readonly FunctionType functionType;
  private readonly double min;
  private readonly double max;

  public RealVectorTestFunctionProblem(FunctionType functionType, double min, double max) 
    : base(GenotypeMapper.Identity<RealVector>(), Goal.Minimize) 
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

  // public override IEvaluator<RealVector, Fitness> CreateEvaluator() {
  //   return new Evaluator(this);
  // }

  // private sealed class Evaluator : EvaluatorBase<RealVector, Fitness>  {
  //   private readonly RealVectorTestFunctionProblem problem;
  //
  //   public Evaluator(RealVectorTestFunctionProblem problem) {
  //     this.problem = problem;
  //   }
  //
  //   public override Fitness Evaluate(RealVector solution) {
  //     return problem.Evaluate(solution);
  //   }
  // }

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
  public RealVectorEncoding CreateRealVectorEncoding() {
    var parameter = CreateRealVectorEncodingParameter();

    return new RealVectorEncoding(parameter) {
      Creator = new UniformDistributedCreator(minimum: null, maximum: null, encodingParameter: parameter), 
      Crossover = new AlphaBetaBlendCrossover(alpha: 0.7, beta: 0.3),
      Mutator = new GaussianMutator(mutationRate: 0.1, mutationStrength: 0.1, parameter)
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
