using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Neighborhoods;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.APIs.RoarNet;

public interface IRoarNetNeighborhood<TG> : Neighbourhood
{
    IEnumerable<IRoarNetMove<TG>> Moves(IRoarNetSolution<TG> solution);
    IRoarNetMove<TG>? RandomMove(IRoarNetSolution<TG> solution);
    IEnumerable<IRoarNetMove<TG>> RandomMoveWithOutReplacement(IRoarNetSolution<TG> solution);
}

public readonly struct RoarNetNeighborhood<TG, TS, TP, TM1>(
    INeighborhood<TG, TM1> neighborhood,
    IRoarNetOperationsProblem<TG, TS, TP> problem,
    ResolutionScope scope,
    IRandomNumberGenerator rng)
    : IRoarNetNeighborhood<TG>
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
{
    private readonly IMoveApplicationExecutor<TG, TM1> applier = scope.For<TG, TS, TP>(problem.Problem).Resolve(neighborhood.MoveApplier);
    private readonly IMoveCreationExecutor<TG, TM1> creator = scope.For<TG, TS, TP>(problem.Problem).Resolve(neighborhood.MoveCreator);
    private readonly IMoveEvaluationExecutor<TG, TM1> evaluator = scope.For<TG, TS, TP>(problem.Problem).Resolve(neighborhood.MoveEvaluator);

    private IRoarNetMove<TG> MakeMove(TM1 move) =>
        new RoarNetMove<TG, TS, TP, TM1>(move, this);

    public IRoarNetSolution<TG> ApplyMove(TM1 move, IRoarNetSolution<TG> solution) =>
        new RoarNetSolution<TG>(applier.Apply(solution.Genotype, move, rng), problem);

    public IEnumerable<IRoarNetMove<TG>> Moves(IRoarNetSolution<TG> solution) =>
        creator.Moves(solution.Genotype, rng).Select(MakeMove);

    public IRoarNetMove<TG>? RandomMove(IRoarNetSolution<TG> solution)
    {
        var m = creator.Moves(solution.Genotype, rng).Shuffle(rng).FirstOrDefault();
        return m is not null ? new RoarNetMove<TG, TS, TP, TM1>(m, this) : null;
    }

    public IEnumerable<IRoarNetMove<TG>> RandomMoveWithOutReplacement(IRoarNetSolution<TG> solution) =>
        creator.Moves(solution.Genotype, rng).Select(MakeMove);

    public double? LowerBoundIncrement(TM1 move, IRoarNetSolution<TG> solution)
    {
        //if (ni is IIncrementalBoundNeighborhoodInstance<TG, TS, TP> bin)
        //    return bin.BoundIncrement(solution.Genotype, move, rng)?[0];
        throw new NotSupportedException();
    }

    public double? ObjectiveValueIncrement(TM1 move, IRoarNetSolution<TG> solution)
    {
        return evaluator.Evaluate(solution.ObjectiveValue!, solution.Genotype, move, rng)[0];
    }

    public IRoarNetSolution<TG> Revert(TM1 move, IRoarNetSolution<TG> solution)
    {
        //if (ni is IReversibleNeighborhoodInstance<TG, TS, TP, TM1> bin)
        //    return new RoarNetSolution<TG>(bin.RevertMove(solution.Genotype, move, problem.SearchSpace, problem.Problem), problem);
        throw new NotSupportedException();
    }
}
