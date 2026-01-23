using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Problems.TestFunctions;

public class MultiObjectiveTestFunctionProblem : RealVectorProblem {
  private readonly IMultiObjectiveTestFunction testFunction;

  public MultiObjectiveTestFunctionProblem(IMultiObjectiveTestFunction testFunction) : base(testFunction.Objective, GeTSearchSpace(testFunction)) {
    this.testFunction = testFunction;
  }

  public MultiObjectiveTestFunctionProblem(IMultiObjectiveTestFunction testFunction, RealVectorSearchSpace searchSpace) : base(testFunction.Objective, searchSpace) {
    ArgumentOutOfRangeException.ThrowIfNotEqual(searchSpace.Length, testFunction.Dimension);
    this.testFunction = testFunction;
  }

  public override ObjectiveVector Evaluate(RealVector solution, IRandomNumberGenerator random) {
    return new ObjectiveVector(testFunction.Evaluate(solution));
  }

  private static RealVectorSearchSpace GeTSearchSpace(IMultiObjectiveTestFunction testFunction) {
    return new RealVectorSearchSpace(testFunction.Dimension, testFunction.Min, testFunction.Max);
  }

  // public override RealVector Decode(RealVector genotype) => genotype;

  // return new RealVectorSearchSpace<RealVector>(Decoder.Identity<RealVector>()) {
  //   Creator = new UniformDistributedCreator(minimum: null, maximum: null), 
  //   Crossover = new AlphaBetaBlendCrossover(alpha: 0.7, beta: 0.3),
  //   Mutator = new GaussianMutator(mutationRate: 0.1, mutationStrength: 0.1)
  // };
}
