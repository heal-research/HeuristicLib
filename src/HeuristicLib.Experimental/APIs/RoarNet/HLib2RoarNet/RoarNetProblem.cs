using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Neighborhoods;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

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

public interface IRoarNetOperationsProblem<TG, out TS, out TP> : IRoarNetProblem<TG>
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
{
    TP Problem { get; }
    TS SearchSpace { get; }
}

public record RoarNetProblem<TG, TS, TP, TM1, TM2, TM3>(
    TP Problem,
    IEvaluationExecutor<TG> Evaluator,
    IEvaluationExecutor<TG> BoundsEvaluator,
    INeighborhood<TG, TM1> ConstructionNeighborhood,
    INeighborhood<TG, TM2> DestructionNeighborhood,
    INeighborhood<TG, TM3> LocalNeighborhood,
    ICreationExecutor<TG> EmptyCreator,
    ICreationExecutor<TG> RandomCreator,
    ICreationExecutor<TG> HeuristicCreator,
    IRandomNumberGenerator Rng,
    ResolutionScope Registry) : IRoarNetOperationsProblem<TG, TS, TP>
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
{
    public RoarNetProblem(
        TP Problem,
        IOperator<Evaluation, TG> Evaluator,
        IOperator<Evaluation, TG> BoundsEvaluator,
        INeighborhood<TG, TM1> ConstructionNeighborhood,
        INeighborhood<TG, TM2> DestructionNeighborhood,
        INeighborhood<TG, TM3> LocalNeighborhood,
        IOperator<Creation, TG> EmptyCreator,
        IOperator<Creation, TG> RandomCreator,
        IOperator<Creation, TG> HeuristicCreator,
        IRandomNumberGenerator Rng,
        ResolutionScope Registry) : this(
        Problem,
        Registry.Resolve<TG, TS, TP>(Evaluator, Problem),
        Registry.Resolve<TG, TS, TP>(BoundsEvaluator, Problem),
        ConstructionNeighborhood,
        DestructionNeighborhood,
        LocalNeighborhood,
        Registry.Resolve<TG, TS, TP>(EmptyCreator, Problem),
        Registry.Resolve<TG, TS, TP>(RandomCreator, Problem),
        Registry.Resolve<TG, TS, TP>(HeuristicCreator, Problem),
        Rng,
        Registry
    )
    { }

    public TP Problem { get; } = Problem;
    public TS SearchSpace { get; } = Problem.SearchSpace;

    public IRoarNetNeighborhood<TG> ConstructionNeighbourhood() => new RoarNetNeighborhood<TG, TS, TP, TM1>(ConstructionNeighborhood, this, Registry, Rng);
    public IRoarNetNeighborhood<TG> DestructionNeighbourhood() => new RoarNetNeighborhood<TG, TS, TP, TM2>(DestructionNeighborhood, this, Registry, Rng);
    public IRoarNetNeighborhood<TG> LocalNeighbourhood() => new RoarNetNeighborhood<TG, TS, TP, TM3>(LocalNeighborhood, this, Registry, Rng);
    public IRoarNetSolution<TG> EmptySolution() => new RoarNetSolution<TG>(EmptyCreator.Create(1, Rng)[0], this);
    public IRoarNetSolution<TG> RandomSolution() => new RoarNetSolution<TG>(RandomCreator.Create(1, Rng)[0], this);
    public IRoarNetSolution<TG> HeuristicSolution() => new RoarNetSolution<TG>(HeuristicCreator.Create(1, Rng)[0], this);
    public double? LowerBound(TG genotype) => BoundsEvaluator.Evaluate([genotype], Rng)[0][0];
    public double? Objective(TG genotype) => Evaluator.Evaluate([genotype], Rng)[0][0];
}
