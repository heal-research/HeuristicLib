using HEAL.HeuristicLib.Genotypes.Vectors;

namespace HEAL.HeuristicLib.Problems.TestFunctions;

public class CombinedGradientTestFunction(params IReadOnlyList<IGradientTestFunction> functions)
  : CombinedTestFunction(functions), IMultiObjectiveGradientTestFunction
{
    private readonly ImmutableArray<IGradientTestFunction> functions = functions.ToImmutableArray();

    public RealVector[] EvaluateGradient(RealVector solution) => functions.Select(x => x.EvaluateGradient(solution)).ToArray();
}
