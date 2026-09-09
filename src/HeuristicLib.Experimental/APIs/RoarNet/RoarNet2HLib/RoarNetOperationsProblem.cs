using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.APIs.RoarNet;

public sealed class RoarNetOperationsProblem(Operations operations, Problem roarNetProblemInstance) : SingleSolutionProblem<RoarNetOperationsProblem, Solution, RoarNetOperationsSearchSpace>(SingleObjective.Minimize, RoarNetOperationsSearchSpace.Instance)
{
    public Operations Operations { get; } = operations;
    public Problem RoarNetProblemInstance { get; } = roarNetProblemInstance;

    public override ObjectiveVector Evaluate(Solution solution, IRandomNumberGenerator random) =>
        Operations.objective_value(solution) ?? throw new InvalidOperationException("ROAR-NET solution is not objectively evaluable.");
}
