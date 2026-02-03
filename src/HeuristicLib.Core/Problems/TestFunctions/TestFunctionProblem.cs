using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Problems.TestFunctions;

// This is an example problem that fully uses the standard search space of real vectors and only the standard operators.
public class TestFunctionProblem : RealVectorProblem
{
  public readonly ITestFunction TestFunction;
  public TestFunctionProblem() : this(null!) {}

  public TestFunctionProblem(ITestFunction testFunction) : base(SingleObjective.Create(testFunction.Objective), GetEncoding(testFunction)) => TestFunction = testFunction;

  public override ObjectiveVector Evaluate(RealVector solution, IRandomNumberGenerator random) => TestFunction.Evaluate(solution);

  private static RealVectorSearchSpace GetEncoding(ITestFunction testFunction) => new(testFunction.Dimension, testFunction.Min, testFunction.Max);
}
