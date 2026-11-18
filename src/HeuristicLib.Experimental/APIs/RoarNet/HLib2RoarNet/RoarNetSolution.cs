namespace HEAL.HeuristicLib.APIs.RoarNet;

public interface IRoarNetSolution<out TG> : Solution
{
    TG Genotype { get; }
    double? LowerBound { get; }
    double? ObjectiveValue { get; }
    IRoarNetSolution<TG> Copy();
}

public readonly struct RoarNetSolution<TG>(TG genotype, IRoarNetProblem<TG> problem) : IRoarNetSolution<TG>
{
    public TG Genotype { get; } = genotype;
    private IRoarNetProblem<TG> Problem { get; } = problem;
    public double? LowerBound => Problem.LowerBound(Genotype);
    public double? ObjectiveValue => Problem.Objective(Genotype);
    public IRoarNetSolution<TG> Copy() => new RoarNetSolution<TG>(Genotype, Problem);
}
