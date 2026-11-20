using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.APIs.RoarNet;

public interface IRoarNetMove<TG> : Move
{
    IRoarNetSolution<TG> Apply(IRoarNetSolution<TG> solution);
    double? LowerBoundIncrement(IRoarNetSolution<TG> solution);
    double? ObjectiveValueIncrement(IRoarNetSolution<TG> solution);
    IRoarNetSolution<TG> Revert(IRoarNetSolution<TG> solution);
}

public readonly struct RoarNetMove<TG, TS, TP, TM1>(TM1 move, RoarNetNeighborhood<TG, TS, TP, TM1> neighborhood) : IRoarNetMove<TG>
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
{
    public IRoarNetSolution<TG> Apply(IRoarNetSolution<TG> solution) => neighborhood.ApplyMove(move, solution);
    public double? LowerBoundIncrement(IRoarNetSolution<TG> solution) => neighborhood.LowerBoundIncrement(move, solution);
    public double? ObjectiveValueIncrement(IRoarNetSolution<TG> solution) => neighborhood.ObjectiveValueIncrement(move, solution);
    public IRoarNetSolution<TG> Revert(IRoarNetSolution<TG> solution) => neighborhood.Revert(move, solution);
}
