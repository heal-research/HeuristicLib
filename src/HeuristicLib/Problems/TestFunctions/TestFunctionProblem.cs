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
}
