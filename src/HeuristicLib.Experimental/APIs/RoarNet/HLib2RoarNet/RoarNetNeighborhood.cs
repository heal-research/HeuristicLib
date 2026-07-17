using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators.MoveAppliers;
using HEAL.HeuristicLib.Operators.MoveCreators;
using HEAL.HeuristicLib.Operators.MoveEvaluators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.APIs.RoarNet;

using Operators.Neighborhoods;

public readonly struct RoarNetNeighborhood<TG, TS, TP, TM1>(
    INeighborhood<TG, TS, TP, TM1> neighborhood,
    IRoarNetOperationsProblem<TG, TS, TP> problem,
    ExecutionInstanceRegistry registry,
    IRandomNumberGenerator rng)
    : IRoarNetNeighborhood<TG>
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
{
    private readonly IMoveApplierInstance<TG, TS, TP, TM1> applier = registry.Resolve(neighborhood.MoveApplier);
    private readonly IMoveCreatorInstance<TG, TS, TP, TM1> creator = registry.Resolve(neighborhood.MoveCreator);
    private readonly IMoveEvaluatorInstance<TG, TS, TP, TM1> evaluator = registry.Resolve(neighborhood.MoveEvaluator);

    private IRoarNetMove<TG> MakeMove(TM1 move)
        => new RoarNetMove<TG, TS, TP, TM1>(move, this);

    public IRoarNetSolution<TG> ApplyMove(TM1 move, IRoarNetSolution<TG> solution)
        => new RoarNetSolution<TG>(applier.Apply(solution.Genotype, move, rng, problem.SearchSpace, problem.Problem), problem);

    public IEnumerable<IRoarNetMove<TG>> Moves(IRoarNetSolution<TG> solution)
        => creator.Moves(solution.Genotype, rng, problem.SearchSpace, problem.Problem).Select(MakeMove);

    public IRoarNetMove<TG>? RandomMove(IRoarNetSolution<TG> solution)
    {
        var m = creator.Moves(solution.Genotype, rng, problem.SearchSpace, problem.Problem).Shuffle(rng).FirstOrDefault();
        return m is not null ? new RoarNetMove<TG, TS, TP, TM1>(m, this) : null;
    }

    public IEnumerable<IRoarNetMove<TG>> RandomMoveWithOutReplacement(IRoarNetSolution<TG> solution)
        => creator.Moves(solution.Genotype, rng, problem.SearchSpace, problem.Problem).Select(MakeMove);

    public double? LowerBoundIncrement(TM1 move, IRoarNetSolution<TG> solution)
    {
        //if (ni is IIncrementalBoundNeighborhoodInstance<TG, TS, TP> bin)
        //    return bin.BoundIncrement(solution.Genotype, move, rng, problem.SearchSpace, problem.Problem)?[0];
        throw new NotSupportedException();
    }

    public double? ObjectiveValueIncrement(TM1 move, IRoarNetSolution<TG> solution)
    {
        return evaluator.Evaluate(solution.ObjectiveValue!, solution.Genotype, move, rng, problem.SearchSpace, problem.Problem)[0];
    }

    public IRoarNetSolution<TG> Revert(TM1 move, IRoarNetSolution<TG> solution)
    {
        //if (ni is IReversibleNeighborhoodInstance<TG, TS, TP, TM1> bin)
        //    return new RoarNetSolution<TG>(bin.RevertMove(solution.Genotype, move, problem.SearchSpace, problem.Problem), problem);
        throw new NotSupportedException();
    }
}
