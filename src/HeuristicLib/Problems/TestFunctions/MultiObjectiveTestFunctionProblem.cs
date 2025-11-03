using HEAL.HeuristicLib.Encodings.RealVector;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Problems.TestFunctions;

public class MultiObjectiveTestFunctionProblem(IMultiObjectiveTestFunction testFunction) : RealVectorProblem(testFunction.Objective, GetEncoding(testFunction)) {
  public override ObjectiveVector Evaluate(RealVector solution, IRandomNumberGenerator random) {
    return new ObjectiveVector(testFunction.Evaluate(solution));
  }

  private static RealVectorEncoding GetEncoding(IMultiObjectiveTestFunction testFunction) {
    return new RealVectorEncoding(testFunction.Dimension, testFunction.Min, testFunction.Max);
  }

  // public override RealVector Decode(RealVector genotype) => genotype;

  // return new RealVectorSearchSpace<RealVector>(Decoder.Identity<RealVector>()) {
  //   Creator = new UniformDistributedCreator(minimum: null, maximum: null), 
  //   Crossover = new AlphaBetaBlendCrossover(alpha: 0.7, beta: 0.3),
  //   Mutator = new GaussianMutator(mutationRate: 0.1, mutationStrength: 0.1)
  // };
}
