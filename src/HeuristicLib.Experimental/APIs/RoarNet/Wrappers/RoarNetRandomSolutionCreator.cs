using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.APIs.RoarNet;

public sealed record RoarNetRandomSolutionCreator
    : StatelessCreator<Solution, RoarNetSearchSpace, RoarNetProblem>
{
    public override IReadOnlyList<Solution> Create(int count, IRandomNumberGenerator random, RoarNetSearchSpace searchSpace, RoarNetProblem problem)
    {
        var p = problem.RoarNetProblemInstance;
        var ops = problem.Operations;
        return Enumerable.Range(0, count)
                         .Select(_ => ops.random_solution(p))
                         .ToArray();
    }
}
