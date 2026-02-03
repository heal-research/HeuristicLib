using HEAL.HeuristicLib.Encodings.RealVector;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Problems.TestFunctions;

public class MultiObjectiveTestFunctionProblem : RealVectorProblem {
  public readonly IMultiObjectiveTestFunction TestFunction;

  public MultiObjectiveTestFunctionProblem(IMultiObjectiveTestFunction testFunction) : base(testFunction.Objective, GetEncoding(testFunction)) {
    TestFunction = testFunction;
  }

  public MultiObjectiveTestFunctionProblem(IMultiObjectiveTestFunction testFunction, RealVectorSearchSpace searchSpace) : base(testFunction.Objective, searchSpace) {
    ArgumentOutOfRangeException.ThrowIfNotEqual(searchSpace.Length, testFunction.Dimension);
    TestFunction = testFunction;
  }

  public override ObjectiveVector Evaluate(RealVector solution, IRandomNumberGenerator random) {
    return new ObjectiveVector(TestFunction.Evaluate(solution));
  }

  private static RealVectorSearchSpace GetEncoding(IMultiObjectiveTestFunction testFunction) {
    return new RealVectorSearchSpace(testFunction.Dimension, testFunction.Min, testFunction.Max);
  }
}
