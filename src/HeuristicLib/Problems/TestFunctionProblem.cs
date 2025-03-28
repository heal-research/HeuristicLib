using HEAL.HeuristicLib.Core;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Problems;

public class TestFunctionProblem : ProblemBase<RealVector, TestFunctionInstance>, IEncodableProblem<RealVector, RealVector, RealVectorEncoding<RealVector>> {
  
  //public override Objective Objective => SingleObjective.Minimize;

  public override Fitness Evaluate(RealVector solution, TestFunctionInstance instance) {
    return instance.Evaluate(solution);
  }
  
  public RealVectorEncoding<RealVector> GetEncoding() {
    return new RealVectorEncoding<RealVector>(Decoder.Identity<RealVector>()) {
      Creator = new UniformDistributedCreator(minimum: null, maximum: null), 
      Crossover = new AlphaBetaBlendCrossover(alpha: 0.7, beta: 0.3),
      Mutator = new GaussianMutator(mutationRate: 0.1, mutationStrength: 0.1)
    };
  }
  
  //
  // public RealVectorEncodingParameter CreateRealVectorEncodingParameter() {
  //   return new RealVectorEncodingParameter(length: 2, min, max);
  // }
  // public RealVectorEncoding CreateRealVectorEncoding() {
  //   var parameter = CreateRealVectorEncodingParameter();
  //
  //   return new RealVectorEncoding(parameter) {
  //    
  //   };
  // }
  
  // public GeneticAlgorithmSpec CreateGeneticAlgorithmDefaultConfig() {
  //   return new GeneticAlgorithmSpec(
  //     Creator: functionType == FunctionType.Sphere ? new UniformRealVectorCreatorSpec() : new NormalRealVectorCreatorSpec([5], [0.5]),
  //     Crossover: new AlphaBetaBlendRealVectorCrossoverSpec(Alpha: 0.8, Beta: 0.2),
  //     Mutator: new GaussianRealVectorMutatorSpec()
  //   );
  // }
}

public class TestFunctionInstance : IBindableProblemInstance<RealVectorEncodingParameter, RealVector> {
  private readonly ITestFunction testFunction;
  
  public TestFunctionInstanceInformation? Information { get; init; }

  public TestFunctionInstance(ITestFunction testFunction, TestFunctionInstanceInformation? information = null) {
    this.testFunction = testFunction;
    Information = information;
  }

  public double Evaluate(RealVector solution) {
    return testFunction.Evaluate(solution);
  }
  
  public Objective GetObjective() => SingleObjective.Minimize;

  public RealVectorEncodingParameter GetEncodingParameter() {
    return new RealVectorEncodingParameter(length: testFunction.Dimension, minimum: testFunction.Min, maximum: testFunction.Max);
  }
}

public class TestFunctionInstanceInformation {
  public required string Name { get; init; }
  public string? Description { get; init; }
  public string? Publication { get; init; }
  public double? BestKnownQuality { get; init; }
  public RealVector? BestKnownSolution { get; init; }
}

public interface ITestFunction {
  public int Dimension { get; }
  double Min { get; }
  double Max { get; }
  double Evaluate(RealVector solution);
}

public class SphereFunction : ITestFunction {
  public int Dimension { get; }
  public double Min => -5.12;
  public double Max => 5.12;


  
  public double Evaluate(RealVector solution) {
    return solution.Sum(x => x * x);
  }
}

public class RastriginFunction : ITestFunction {
  public int Dimension { get; }
  public double Min => -5.12;
  public double Max => 5.12;

  public RastriginFunction(int dimension) {
    Dimension = dimension;
  }
  
  public double Evaluate(RealVector solution) {
    int n = solution.Count;
    double A = 10;
    double sum = A * n;
    for (int i = 0; i < n; i++) {
      sum += solution[i] * solution[i] - A * Math.Cos(2 * Math.PI * solution[i]);
    }
    return sum;
  }
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
