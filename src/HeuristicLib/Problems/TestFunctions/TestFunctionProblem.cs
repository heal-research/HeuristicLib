using HEAL.HeuristicLib.Encodings.RealVector;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Problems.TestFunctions;

// This is an example problem that fully uses the standard search space of real vectors and only the standard operators.
public class TestFunctionProblem : RealVectorProblem {
  public readonly ITestFunction TestFunction;
  public TestFunctionProblem() : this(null!) { }

  public TestFunctionProblem(ITestFunction testFunction) : base(SingleObjective.Create(testFunction.Objective), GetEncoding(testFunction)) {
    TestFunction = testFunction;
  }

  public override ObjectiveVector Evaluate(RealVector solution, IRandomNumberGenerator random) {
    return TestFunction.Evaluate(solution);
  }

  private static RealVectorEncoding GetEncoding(ITestFunction testFunction) {
    return new RealVectorEncoding(testFunction.Dimension, testFunction.Min, testFunction.Max);
  }

  //public override RealVector Decode(RealVector genotype) => genotype;

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
