using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.APIs.RoarNet;

public sealed record RoarNetLocalMoveMutator : SingleSolutionMutator<Solution, RoarNetSearchSpace, RoarNetProblem>
{
    public override Solution Mutate(Solution parent, IRandomNumberGenerator random, RoarNetSearchSpace searchSpace, RoarNetProblem problem)
    {
        var neighbourhood = problem.Operations.local_neighbourhood(problem.RoarNetProblemInstance);
        var move = problem.Operations.random_move(neighbourhood, parent);
        var working = problem.Operations.copy_solution(parent); // avoid potential destruction of parent
        return move is null ? working : problem.Operations.apply_move(move, working);
    }
}
