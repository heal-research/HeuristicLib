using HEAL.HeuristicLib.Encodings.RealVector;

namespace HEAL.HeuristicLib.Problems.TestFunctions;

public interface IGradientTestFunction : ITestFunction {
  RealVector EvaluateGradient(RealVector solution);
}
