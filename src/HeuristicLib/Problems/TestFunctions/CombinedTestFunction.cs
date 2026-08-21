using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Objectives;

namespace HEAL.HeuristicLib.Problems.TestFunctions;

public class CombinedTestFunction : IMultiObjectiveTestFunction
{
    public CombinedTestFunction(params IReadOnlyList<ITestFunction> functions)
    {
        Functions = functions.ToImmutableArray();
        Dimension = Functions.Select(f => f.Dimension).Distinct().Single();
        Min = Functions.Select(f => f.Min).Max();
        Max = Functions.Select(f => f.Max).Min();
        Objective = MultiObjective.Create(Functions.Select(f => f.Objective).ToArray());
    }

    public ImmutableArray<ITestFunction> Functions { get; }
    public int Dimension { get; }
    public double Min { get; }
    public double Max { get; }
    public ObjectiveDirections Objective { get; }

    public RealVector Evaluate(RealVector solution) => new(Functions.Select(x => x.Evaluate(solution)));
}
