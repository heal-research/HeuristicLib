using HEAL.HeuristicLib.Encodings.RealVector;

namespace HEAL.HeuristicLib.Problems.TestFunctions;

public interface IMultiObjectiveGradientTestFunction : IMultiObjectiveTestFunction {
  RealVector[] EvaluateGradient(RealVector solution);
}
