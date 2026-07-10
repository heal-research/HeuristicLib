namespace HEAL.HeuristicLib.APIs.RoarNet;

public readonly struct RoarNetSolution<TG>(TG genotype, IRoarNetProblem<TG> problem) : IRoarNetSolution<TG>
{
    public TG Genotype { get; } = genotype;
    private IRoarNetProblem<TG> Problem { get; } = problem;
    public double? LowerBound => Problem.LowerBound(Genotype);
    public double? ObjectiveValue => Problem.Objective(Genotype);
    public IRoarNetSolution<TG> Copy() => new RoarNetSolution<TG>(Genotype, Problem);
}
