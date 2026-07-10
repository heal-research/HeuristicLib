namespace HEAL.HeuristicLib.APIs.RoarNet;

public interface IRoarNetSolution<out TG> : Solution
{
    TG Genotype { get; }
    double? LowerBound { get; }
    double? ObjectiveValue { get; }
    IRoarNetSolution<TG> Copy();
};
