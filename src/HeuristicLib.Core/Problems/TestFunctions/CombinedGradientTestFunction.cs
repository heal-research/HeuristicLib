using HEAL.HeuristicLib.Genotypes.Vectors;

namespace HEAL.HeuristicLib.Problems.TestFunctions;

public class CombinedGradientTestFunction(params IReadOnlyCollection<IGradientTestFunction> functions)
  : CombinedTestFunction(functions), IMultiObjectiveGradientTestFunction
{
  private readonly IGradientTestFunction[] functions = functions.ToArray();

  public RealVector[] EvaluateGradient(RealVector solution) => functions.Select(x => x.EvaluateGradient(solution)).ToArray();
}
