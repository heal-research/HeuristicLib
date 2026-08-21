namespace HEAL.HeuristicLib.APIs.RoarNet;

#region Wrapping a HeuristicLib problem + Operators to a ROAR-NET operations bundle
public record RoarNetOperations<TG> : IRoarNetOperations<TG>
{
    public static RoarNetOperations<TG> Instance { get; } = new();
    private RoarNetOperations() { }

    #region typeless
    public Solution apply_move(Move move, Solution solution) => apply_move((IRoarNetMove<TG>)move, (IRoarNetSolution<TG>)solution);

    public Neighbourhood construction_neighbourhood(Problem problem) => construction_neighbourhood((IRoarNetProblem<TG>)problem);

    public Solution copy_solution(Solution solution) => copy_solution((IRoarNetSolution<TG>)solution);

    public Neighbourhood destruction_neighbourhood(Problem problem) => destruction_neighbourhood((IRoarNetProblem<TG>)problem);

    public Solution empty_solution(Problem problem) => empty_solution((IRoarNetProblem<TG>)problem);

    public Solution? heuristic_solution(Problem problem) => heuristic_solution((IRoarNetProblem<TG>)problem);

    public Neighbourhood local_neighbourhood(Problem problem) => local_neighbourhood((IRoarNetProblem<TG>)problem);

    public double? lower_bound(Solution solution) => lower_bound((IRoarNetSolution<TG>)solution);

    public double? lower_bound_increment(Move move, Solution solution) => lower_bound_increment((IRoarNetMove<TG>)move, (IRoarNetSolution<TG>)solution);

    public IEnumerable<Move> moves(Neighbourhood neighbourhood, Solution solution) => Moves((IRoarNetNeighborhood<TG>)neighbourhood, (IRoarNetSolution<TG>)solution);

    public double? objective_value(Solution solution) => objective_value((IRoarNetSolution<TG>)solution);

    public double? objective_value_increment(Move move, Solution solution) => objective_value_increment((IRoarNetMove<TG>)move, (IRoarNetSolution<TG>)solution);

    public Move? random_move(Neighbourhood neighbourhood, Solution solution) => random_move((IRoarNetNeighborhood<TG>)neighbourhood, (IRoarNetSolution<TG>)solution);

    public IEnumerable<Move> random_moves_without_replacement(Neighbourhood neighbourhood, Solution solution) => random_moves_without_replacement((IRoarNetNeighborhood<TG>)neighbourhood, (IRoarNetSolution<TG>)solution);

    public Solution random_solution(Problem problem) => random_solution((IRoarNetProblem<TG>)problem);

    public Solution revert_move(Move move, Solution solution) => revert_move((IRoarNetMove<TG>)move, (IRoarNetSolution<TG>)solution);
    #endregion

    //from here typed operations

    public IRoarNetSolution<TG> apply_move(IRoarNetMove<TG> roarNetMove, IRoarNetSolution<TG> solution) => roarNetMove.Apply(solution);

    public IRoarNetNeighborhood<TG> construction_neighbourhood(IRoarNetProblem<TG> problem) => problem.ConstructionNeighbourhood();

    public IRoarNetSolution<TG> copy_solution(IRoarNetSolution<TG> solution) => solution.Copy();

    public IRoarNetNeighborhood<TG> destruction_neighbourhood(IRoarNetProblem<TG> problem) => problem.DestructionNeighbourhood();

    public IRoarNetSolution<TG> empty_solution(IRoarNetProblem<TG> problem) => problem.EmptySolution();

    public IRoarNetSolution<TG>? heuristic_solution(IRoarNetProblem<TG> problem) => problem.HeuristicSolution();

    public IRoarNetNeighborhood<TG> local_neighbourhood(IRoarNetProblem<TG> problem) => problem.LocalNeighbourhood();

    public double? lower_bound(IRoarNetSolution<TG> solution) => solution.LowerBound;

    public double? lower_bound_increment(IRoarNetMove<TG> move, IRoarNetSolution<TG> solution) => move.LowerBoundIncrement(solution);

    public IEnumerable<IRoarNetMove<TG>> Moves(IRoarNetNeighborhood<TG> neighbourhood, IRoarNetSolution<TG> solution) => neighbourhood.Moves(solution);

    public double? objective_value(IRoarNetSolution<TG> solution) => solution.ObjectiveValue;

    public double? objective_value_increment(IRoarNetMove<TG> move, IRoarNetSolution<TG> solution) => move.ObjectiveValueIncrement(solution);

    public IRoarNetMove<TG>? random_move(IRoarNetNeighborhood<TG> neighbourhood, IRoarNetSolution<TG> solution) => neighbourhood.RandomMove(solution);

    public IEnumerable<IRoarNetMove<TG>> random_moves_without_replacement(IRoarNetNeighborhood<TG> neighbourhood, IRoarNetSolution<TG> solution) => neighbourhood.RandomMoveWithOutReplacement(solution);

    public IRoarNetSolution<TG> random_solution(IRoarNetProblem<TG> problem) => problem.RandomSolution();

    public IRoarNetSolution<TG> revert_move(IRoarNetMove<TG> move, IRoarNetSolution<TG> solution) => move.Revert(solution);
}
#endregion
