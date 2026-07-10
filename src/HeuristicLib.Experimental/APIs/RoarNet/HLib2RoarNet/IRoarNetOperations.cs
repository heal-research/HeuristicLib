namespace HEAL.HeuristicLib.APIs.RoarNet;

public interface IRoarNetOperations<TSolution> : Operations
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
    IEnumerable<IRoarNetMove<TSolution>> Moves(IRoarNetNeighborhood<TSolution> neighbourhood, IRoarNetSolution<TSolution> solution);
    double? objective_value(IRoarNetSolution<TSolution> solution);
    double? objective_value_increment(IRoarNetMove<TSolution> move, IRoarNetSolution<TSolution> solution);
    IRoarNetMove<TSolution>? random_move(IRoarNetNeighborhood<TSolution> neighbourhood, IRoarNetSolution<TSolution> solution);
    IEnumerable<IRoarNetMove<TSolution>> random_moves_without_replacement(IRoarNetNeighborhood<TSolution> neighbourhood, IRoarNetSolution<TSolution> solution);
    IRoarNetSolution<TSolution> random_solution(IRoarNetProblem<TSolution> problem);
    IRoarNetSolution<TSolution> revert_move(IRoarNetMove<TSolution> move, IRoarNetSolution<TSolution> solution);
}
