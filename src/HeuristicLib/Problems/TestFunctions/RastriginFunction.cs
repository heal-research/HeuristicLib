using HEAL.HeuristicLib.Encodings.RealVector;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems.TestFunctions;

public class RastriginFunction(int dimension) : ITestFunction {
  public int Dimension { get; } = dimension;
  public double Min => -5.12;
  public double Max => 5.12;
  public ObjectiveDirection Objective => ObjectiveDirection.Minimize;

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
