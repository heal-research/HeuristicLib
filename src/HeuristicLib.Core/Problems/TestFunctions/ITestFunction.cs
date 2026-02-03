using HEAL.HeuristicLib.Encodings.RealVector;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems.TestFunctions;

public interface ITestFunction {
  public int Dimension { get; }
  double Min { get; }
  double Max { get; }
  public ObjectiveDirection Objective { get; }
  double Evaluate(RealVector solution);
}
