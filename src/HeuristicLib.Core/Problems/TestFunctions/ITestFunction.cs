using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems.TestFunctions;

public interface ITestFunction
{
  int Dimension { get; }
  double Min { get; }
  double Max { get; }
  ObjectiveDirection Objective { get; }
  double Evaluate(RealVector solution);
}
