using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.APIs.RoarNet;

using Operators.Neighborhoods;

public record RoarNetProblem<TG, TS, TP, TM1, TM2, TM3>(
    TP Problem,
    IEvaluatorInstance<TG, TS, TP> Evaluator,
    IEvaluatorInstance<TG, TS, TP> BoundsEvaluator,
    INeighborhood<TG, TS, TP, TM1> ConstructionNeighborhood,
    INeighborhood<TG, TS, TP, TM2> DestructionNeighborhood,
    INeighborhood<TG, TS, TP, TM3> LocalNeighborhood,
    ICreatorInstance<TG, TS, TP> EmptyCreator,
    ICreatorInstance<TG, TS, TP> RandomCreator,
    ICreatorInstance<TG, TS, TP> HeuristicCreator,
    IRandomNumberGenerator Rng,
    ExecutionInstanceRegistry Registry) : IRoarNetOperationsProblem<TG, TS, TP>
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
{
    public RoarNetProblem(
        TP Problem,
        IEvaluator<TG, TS, TP> Evaluator,
        IEvaluator<TG, TS, TP> BoundsEvaluator,
        INeighborhood<TG, TS, TP, TM1> ConstructionNeighborhood,
        INeighborhood<TG, TS, TP, TM2> DestructionNeighborhood,
        INeighborhood<TG, TS, TP, TM3> LocalNeighborhood,
        ICreator<TG, TS, TP> EmptyCreator,
        ICreator<TG, TS, TP> RandomCreator,
        ICreator<TG, TS, TP> HeuristicCreator,
        IRandomNumberGenerator Rng,
        ExecutionInstanceRegistry Registry) : this(
        Problem,
        Registry.Resolve(Evaluator),
        Registry.Resolve(BoundsEvaluator),
        ConstructionNeighborhood,
        DestructionNeighborhood,
        LocalNeighborhood,
        Registry.Resolve(EmptyCreator),
        Registry.Resolve(RandomCreator),
        Registry.Resolve(HeuristicCreator),
        Rng,
        Registry
    )
    { }

    public TP Problem { get; } = Problem;
    public TS SearchSpace { get; } = Problem.SearchSpace;

    public IRoarNetNeighborhood<TG> ConstructionNeighbourhood() => new RoarNetNeighborhood<TG, TS, TP, TM1>(ConstructionNeighborhood, this, Registry, Rng);
    public IRoarNetNeighborhood<TG> DestructionNeighbourhood() => new RoarNetNeighborhood<TG, TS, TP, TM2>(DestructionNeighborhood, this, Registry, Rng);
    public IRoarNetNeighborhood<TG> LocalNeighbourhood() => new RoarNetNeighborhood<TG, TS, TP, TM3>(LocalNeighborhood, this, Registry, Rng);
    public IRoarNetSolution<TG> EmptySolution() => new RoarNetSolution<TG>(EmptyCreator.Create(1, Rng, SearchSpace, Problem)[0], this);
    public IRoarNetSolution<TG> RandomSolution() => new RoarNetSolution<TG>(RandomCreator.Create(1, Rng, SearchSpace, Problem)[0], this);
    public IRoarNetSolution<TG> HeuristicSolution() => new RoarNetSolution<TG>(HeuristicCreator.Create(1, Rng, SearchSpace, Problem)[0], this);
    public double? LowerBound(TG genotype) => BoundsEvaluator.Evaluate([genotype], Rng, SearchSpace, Problem)[0][0];
    public double? Objective(TG genotype) => Evaluator.Evaluate([genotype], Rng, SearchSpace, Problem)[0][0];
}
