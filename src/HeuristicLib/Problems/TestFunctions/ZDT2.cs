using HEAL.HeuristicLib.Encodings.RealVector;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems.TestFunctions;

public class ZDT2(int dimension) : IMultiObjectiveTestFunction {
  public int Dimension { get; } = dimension;
  public double Min => 0;
  public double Max => 1;
  public Objective Objective => MultiObjective.Create([ObjectiveDirection.Minimize, ObjectiveDirection.Minimize]);

  public RealVector Evaluate(RealVector solution) {
    double g = 0;
    for (int i = 1; i < solution.Count; i++) g += solution[i];
    g = 1.0 + 9.0 * g / (solution.Count - 1);
    double d = solution[0] / g;
    double f0 = solution[0];
    double f1 = g * (1.0 - d * d);
    return new RealVector(f0, f1);
  }
}
