using HEAL.HeuristicLib.Encodings.RealVectors;

namespace HEAL.HeuristicLib.Problems.TestFunctions;

public interface IMultiObjectiveGradientTestFunction : IMultiObjectiveTestFunction
{
    RealVector[] EvaluateGradient(RealVector solution);
}
