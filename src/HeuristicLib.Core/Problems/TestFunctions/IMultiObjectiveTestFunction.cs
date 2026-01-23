using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems.TestFunctions;

public interface IMultiObjectiveTestFunction
{
  int Dimension { get; }
  double Min { get; }
  double Max { get; }
  Objective Objective { get; }
  RealVector Evaluate(RealVector solution);
}
