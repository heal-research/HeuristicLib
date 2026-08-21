namespace HEAL.HeuristicLib.APIs.RoarNet;

public interface IRoarNetNeighborhood<TG> : Neighbourhood
{
    IEnumerable<IRoarNetMove<TG>> Moves(IRoarNetSolution<TG> solution);
    IRoarNetMove<TG>? RandomMove(IRoarNetSolution<TG> solution);
    IEnumerable<IRoarNetMove<TG>> RandomMoveWithOutReplacement(IRoarNetSolution<TG> solution);
}
