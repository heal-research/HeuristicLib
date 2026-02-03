using HEAL.HeuristicLib.Genotypes.Vectors;

namespace HEAL.HeuristicLib.Problems.TestFunctions;

public interface IGradientTestFunction : ITestFunction
{
  RealVector EvaluateGradient(RealVector solution);
}
