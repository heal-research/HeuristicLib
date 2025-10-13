using HEAL.HeuristicLib.Genotypes;
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
