using HEAL.HeuristicLib.Genotypes;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems.TestFunctions;

public interface IMultiObjectiveTestFunction {
  public int Dimension { get; }
  double Min { get; }
  double Max { get; }
  public Objective Objective { get; }
  RealVector Evaluate(RealVector solution);
}
