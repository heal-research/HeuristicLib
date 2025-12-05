using HEAL.HeuristicLib.Encodings.RealVector;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems.TestFunctions;

public class SphereFunction(int dimension) : ITestFunction {
  public int Dimension { get; } = dimension;
  public double Min => -5.12;
  public double Max => 5.12;
  public ObjectiveDirection Objective => ObjectiveDirection.Minimize;

  public double Evaluate(RealVector solution) {
    return solution.Sum(x => x * x);
  }
}

public static class TestFunctionExtensions {
  public static ITestFunction Shifted(this ITestFunction inner, RealVector shiftVector) {
    return new ShiftedTestFunction(shiftVector, inner);
  }

  //public static ITestFunction Shifted(this ITestFunction inner, params double[] shiftVector) {
  //  var d = new double[inner.Dimension];
  //  for (int i = 0; i < d.Length; i++)
  //    d[i] = shiftVector[i % shiftVector.Length];
  //  return new ShiftedTestFunction(new RealVector(d), inner);
  //}
}
