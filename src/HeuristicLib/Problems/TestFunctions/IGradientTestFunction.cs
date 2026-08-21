using HEAL.HeuristicLib.Encodings.RealVectors;

namespace HEAL.HeuristicLib.Problems.TestFunctions;

public interface IGradientTestFunction : ITestFunction
{
    RealVector EvaluateGradient(RealVector solution);
}
