using HEAL.HeuristicLib.Encodings.RealVector;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems.TestFunctions;

public interface IMultiObjectiveTestFunction {
  public int Dimension { get; }
  double Min { get; }
  double Max { get; }
  public Objective Objective { get; }
  RealVector Evaluate(RealVector solution);
}

public interface IMultiObjectiveGradientTestFunction : IMultiObjectiveTestFunction {
  RealVector[] EvaluateGradient(RealVector solution);
}
