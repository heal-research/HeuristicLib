// ReSharper disable InconsistentNaming

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
