using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems;

public class TestFunctionProblem : EncodedProblemBase<RealVector, RealVector, RealVectorSearchSpace> {
  private readonly ITestFunction testFunction;

  public TestFunctionProblem(ITestFunction testFunction) 
    : base(SingleObjective.Create(testFunction.Objective), GetSearchSpace(testFunction)) 
  {
    this.testFunction = testFunction;
  }
  
  public override Fitness Evaluate(RealVector solution) {
    return testFunction.Evaluate(solution);
  }
  
  private static RealVectorSearchSpace GetSearchSpace(ITestFunction testFunction) => new RealVectorSearchSpace(testFunction.Dimension, testFunction.Min, testFunction.Max);
  
  public override RealVector Decode(RealVector genotype) => genotype;
    // return new RealVectorSearchSpace<RealVector>(Decoder.Identity<RealVector>()) {
    //   Creator = new UniformDistributedCreator(minimum: null, maximum: null), 
    //   Crossover = new AlphaBetaBlendCrossover(alpha: 0.7, beta: 0.3),
    //   Mutator = new GaussianMutator(mutationRate: 0.1, mutationStrength: 0.1)
    // };
  
  //
  // public RealVectorSearchSpace CreateRealVectorSearchSpace() {
  //   return new RealVectorSearchSpace(length: 2, min, max);
  // }
  // public RealVectorSearchSpace CreateRealVectorSearchSpace() {
  //   var parameter = CreateRealVectorSearchSpace();
  //
  //   return new RealVectorSearchSpace(parameter) {
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


public interface ITestFunction {
  public int Dimension { get; }
  double Min { get; }
  double Max { get; }
  public ObjectiveDirection Objective { get; }
  double Evaluate(RealVector solution);
}

public class SphereFunction : ITestFunction {
  public int Dimension { get; }
  public double Min => -5.12;
  public double Max => 5.12;
  public ObjectiveDirection Objective => ObjectiveDirection.Minimize;

  public SphereFunction(int dimension) {
    Dimension = dimension;
  }
  
  public double Evaluate(RealVector solution) {
    return solution.Sum(x => x * x);
  }
}

public class RastriginFunction : ITestFunction {
  public int Dimension { get; }
  public double Min => -5.12;
  public double Max => 5.12;
  public ObjectiveDirection Objective => ObjectiveDirection.Minimize;

  
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
//   public static GeneticAlgorithmBuilder<RealVectorSearchSpace, RealVector> WithProblemSearchSpace
//     (this GeneticAlgorithmBuilder<RealVectorSearchSpace, RealVector> builder, RealVectorTestFunctionProblem problem)
//   {
//     builder.WithEvaluator(problem.CreateEvaluator());
//     builder.WithSearchSpace(problem.CreateRealVectorSearchSpaceSearchSpace());
//     builder.WithSpecs(problem.CreateGeneticAlgorithmDefaultConfig());
//     return builder;
//     }
// }
