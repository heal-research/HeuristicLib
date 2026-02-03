using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Problems.TestFunctions;

public class MultiObjectiveTestFunctionProblem : RealVectorProblem
{
  public readonly IMultiObjectiveTestFunction TestFunction;

  public MultiObjectiveTestFunctionProblem(IMultiObjectiveTestFunction testFunction) : base(testFunction.Objective, GetEncoding(testFunction)) => TestFunction = testFunction;

  public MultiObjectiveTestFunctionProblem(IMultiObjectiveTestFunction testFunction, RealVectorSearchSpace searchSpace) : base(testFunction.Objective, searchSpace)
  {
    ArgumentOutOfRangeException.ThrowIfNotEqual(searchSpace.Length, testFunction.Dimension);
    TestFunction = testFunction;
  }

  public override ObjectiveVector Evaluate(RealVector solution, IRandomNumberGenerator random) => new(TestFunction.Evaluate(solution));

  private static RealVectorSearchSpace GetEncoding(IMultiObjectiveTestFunction testFunction) => new(testFunction.Dimension, testFunction.Min, testFunction.Max);
}
