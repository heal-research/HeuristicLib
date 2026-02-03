using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives;

public class SphereFunction(int dimension) : IGradientTestFunction
{
  public int Dimension { get; } = dimension;
  public double Min => -5.12;
  public double Max => 5.12;
  public ObjectiveDirection Objective => ObjectiveDirection.Minimize;

  public double Evaluate(RealVector solution) => solution.Sum(x => x * x);

  public RealVector EvaluateGradient(RealVector solution)
  {
    var g = new double[solution.Count];
    for (var i = 0; i < solution.Count; i++) {
      g[i] = 2 * solution[i];
    }

    return g;
  }
}
