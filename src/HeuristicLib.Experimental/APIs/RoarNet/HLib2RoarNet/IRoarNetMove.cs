namespace HEAL.HeuristicLib.APIs.RoarNet;

public interface IRoarNetMove<TG> : Move
{
    IRoarNetSolution<TG> Apply(IRoarNetSolution<TG> solution);
    double? LowerBoundIncrement(IRoarNetSolution<TG> solution);
    double? ObjectiveValueIncrement(IRoarNetSolution<TG> solution);
    IRoarNetSolution<TG> Revert(IRoarNetSolution<TG> solution);
}
