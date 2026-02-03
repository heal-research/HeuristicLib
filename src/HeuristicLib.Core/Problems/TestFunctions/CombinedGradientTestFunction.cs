using HEAL.HeuristicLib.Encodings.RealVector;

namespace HEAL.HeuristicLib.Problems.TestFunctions;

public class CombinedGradientTestFunction(params IReadOnlyCollection<IGradientTestFunction> functions)
  : CombinedTestFunction(functions), IMultiObjectiveGradientTestFunction {
  private readonly IGradientTestFunction[] functions = functions.ToArray();

  public RealVector[] EvaluateGradient(RealVector solution) {
    return functions.Select(x => x.EvaluateGradient(solution)).ToArray();
  }
}
