// ReSharper disable InconsistentNaming

using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.Partial;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

#pragma warning disable S101
namespace HEAL.HeuristicLib.APIs.RoarNet;

public interface Operations
{
    Solution apply_move(Move move, Solution solution);
    Neighbourhood construction_neighbourhood(Problem problem);
    Solution copy_solution(Solution solution);
    Neighbourhood destruction_neighbourhood(Problem problem);
    Solution empty_solution(Problem problem);
    Solution? heuristic_solution(Problem problem);
    Neighbourhood local_neighbourhood(Problem problem);
    double? lower_bound(Solution solution);
    double? lower_bound_increment(Move move, Solution solution);
    IEnumerable<Move> moves(Neighbourhood neighbourhood, Solution solution);
    double? objective_value(Solution solution);
    double? objective_value_increment(Move move, Solution solution);
    Move? random_move(Neighbourhood neighbourhood, Solution solution);
    IEnumerable<Move> random_moves_without_replacement(Neighbourhood neighbourhood, Solution solution);
    Solution random_solution(Problem problem);
    Solution revert_move(Move move, Solution solution);
}

public record RoarnetProblem : Problem
{ }

public interface Operations<TSolution> : Operations
{
    IRoarNetSolution<TSolution> apply_move(IRoarNetMove<TSolution> roarNetMove, IRoarNetSolution<TSolution> solution);
    IRoarNetNeighborhood<TSolution> construction_neighbourhood(IRoarNetProblem<TSolution> problem);
    IRoarNetSolution<TSolution> copy_solution(IRoarNetSolution<TSolution> solution);
    IRoarNetNeighborhood<TSolution> destruction_neighbourhood(IRoarNetProblem<TSolution> problem);
    IRoarNetSolution<TSolution> empty_solution(IRoarNetProblem<TSolution> problem);
    IRoarNetSolution<TSolution>? heuristic_solution(IRoarNetProblem<TSolution> problem);
    IRoarNetNeighborhood<TSolution> local_neighbourhood(IRoarNetProblem<TSolution> problem);
    double? lower_bound(IRoarNetSolution<TSolution> solution);
    double? lower_bound_increment(IRoarNetMove<TSolution> move, IRoarNetSolution<TSolution> solution);
    IEnumerable<IRoarNetMove<TSolution>> moves(IRoarNetNeighborhood<TSolution> neighbourhood, IRoarNetSolution<TSolution> solution);

    IEnumerable<TMove> moves<TMove>(IRoarNetNeighborhood<TSolution> neighbourhood, IRoarNetSolution<TSolution> solution);
    double? objective_value(IRoarNetSolution<TSolution> solution);
    double? objective_value_increment(IRoarNetMove<TSolution> move, IRoarNetSolution<TSolution> solution);
    IRoarNetMove<TSolution>? random_move(IRoarNetNeighborhood<TSolution> neighbourhood, IRoarNetSolution<TSolution> solution);
    IEnumerable<IRoarNetMove<TSolution>> random_moves_without_replacement(IRoarNetNeighborhood<TSolution> neighbourhood, IRoarNetSolution<TSolution> solution);
    IRoarNetSolution<TSolution> random_solution(IRoarNetProblem<TSolution> problem);
    IRoarNetSolution<TSolution> revert_move(IRoarNetMove<TSolution> move, IRoarNetSolution<TSolution> solution);
}
