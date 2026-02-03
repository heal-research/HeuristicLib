using HEAL.HeuristicLib.Genotypes.Vectors;

namespace HEAL.HeuristicLib.Problems.TestFunctions;

public interface IMultiObjectiveGradientTestFunction : IMultiObjectiveTestFunction
{
  RealVector[] EvaluateGradient(RealVector solution);
}
