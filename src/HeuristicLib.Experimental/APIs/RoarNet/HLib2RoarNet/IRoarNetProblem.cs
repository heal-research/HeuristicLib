namespace HEAL.HeuristicLib.APIs.RoarNet;

public interface IRoarNetProblem<TG> : Problem
{
    IRoarNetNeighborhood<TG> ConstructionNeighbourhood();
    IRoarNetNeighborhood<TG> DestructionNeighbourhood();
    IRoarNetNeighborhood<TG> LocalNeighbourhood();

    IRoarNetSolution<TG> EmptySolution();
    IRoarNetSolution<TG> RandomSolution();
    IRoarNetSolution<TG>? HeuristicSolution();
    double? LowerBound(TG genotype);
    double? Objective(TG genotype);
}
