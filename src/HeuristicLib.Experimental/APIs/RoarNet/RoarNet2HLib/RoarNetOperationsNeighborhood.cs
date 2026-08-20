//using System.Diagnostics.CodeAnalysis;
//using HEAL.HeuristicLib.Operators;
//using HEAL.HeuristicLib.Optimization;
//using HEAL.HeuristicLib.Problems.Partial;
//using HEAL.HeuristicLib.Random;

//namespace HEAL.HeuristicLib.APIs.RoarNet;

//public abstract record RoarNetOperationsNeighborhood(RoarNetOperationsProblem OperationsProblem) : FullFeatureNeighborhood<Solution, RoarNetOperationsSearchSpace, RoarNetOperationsProblem, RoarNetOperationsNeighborhood.State>
//{
//    public record State(Neighbourhood Neighbourhood);

//    protected override State CreateInitialState()
//        => new(GetNeighborhood());

//    protected abstract Neighbourhood GetNeighborhood();

//    protected override IEnumerable<Move> Moves(Solution genotype, State executionState, IRandomNumberGenerator random, RoarNetOperationsSearchSpace operationsSearchSpace, RoarNetOperationsProblem operationsProblem)
//        => operationsProblem.Operations.moves(executionState.Neighbourhood, genotype);

//    protected override bool RandomMove(Solution genotype, State executionState, IRandomNumberGenerator random, RoarNetOperationsSearchSpace operationsSearchSpace, RoarNetOperationsProblem operationsProblem, [MaybeNullWhen(false)] out Move move)
//    {
//        move = operationsProblem.Operations.random_move(executionState.Neighbourhood, genotype);
//        return move != null;
//    }

//    protected override Solution ApplyMove(Solution genotype, Move move, State executionState, RoarNetOperationsSearchSpace operationsSearchSpace, RoarNetOperationsProblem operationsProblem)
//        => operationsProblem.Operations.apply_move(move, genotype);

//    protected override Solution RevertMove(Solution genotype, Move move, State executionState, RoarNetOperationsSearchSpace operationsSearchSpace, RoarNetOperationsProblem operationsProblem)
//        => operationsProblem.Operations.revert_move(move, genotype);

//    protected override ObjectiveVector? EvaluateIncrement(Solution genotype, Move move, State executionState, IRandomNumberGenerator random, RoarNetOperationsSearchSpace operationsSearchSpace, RoarNetOperationsProblem operationsProblem)
//        => operationsProblem.Operations.objective_value_increment(move, genotype);

//    protected override ObjectiveVector? BoundIncrement(Solution genotype, Move move, State executionState, IRandomNumberGenerator random, RoarNetOperationsSearchSpace operationsSearchSpace, RoarNetOperationsProblem operationsProblem)
//        => operationsProblem.Operations.lower_bound_increment(move, genotype);
//}
