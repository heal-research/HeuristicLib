using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.APIs.RoarNet;

public sealed class RoarNetProblem(Operations operations, Problem roarNetProblemInstance) : SingleSolutionProblem<Solution, RoarNetSearchSpace>(SingleObjective.Minimize, new RoarNetSearchSpace(operations))
{
    public Operations Operations { get; } = operations;
    public Problem RoarNetProblemInstance { get; } = roarNetProblemInstance;

    public override ObjectiveVector Evaluate(Solution solution, IRandomNumberGenerator random) => Operations.objective_value(solution)
                                                                                                  ?? throw new InvalidOperationException("ROAR-NET solution is not objectively evaluable.");
}
