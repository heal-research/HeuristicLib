using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.Partial;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

// ReSharper disable InconsistentNaming

#pragma warning disable S101
namespace HEAL.HeuristicLib.APIs.RoarNet;

public interface Operations
{
    Solution apply_move(Move move, Solution solution);
    Neighbourhood construction_neighbourhood(Problem problem);
    Solution copy_solution(Solution solution);
    Neighbourhood destruction_neighbourhood(Problem problem);
    Solution empty_solution(Problem problem);
    Solution? heuristic_solution(Problem problem);
    Neighbourhood local_neighbourhood(Problem problem);
    double? lower_bound(Solution solution);
    double? lower_bound_increment(Move move, Solution solution);
    IEnumerable<Move> moves(Neighbourhood neighbourhood, Solution solution);
    double? objective_value(Solution solution);
    double? objective_value_increment(Move move, Solution solution);
    Move? random_move(Neighbourhood neighbourhood, Solution solution);
    IEnumerable<Move> random_moves_without_replacement(Neighbourhood neighbourhood, Solution solution);
    Solution random_solution(Problem problem);
    Solution revert_move(Move move, Solution solution);
}

#region Wrapping a HeuristicLib problem + Operators to a ROAR-NET operations bundle
public interface IRoarNetOperations<TSolution> : Operations
{
    IRoarNetSolution<TSolution> apply_move(IRoarNetMove<TSolution> roarNetMove, IRoarNetSolution<TSolution> solution);
    IRoarNetNeighborhood<TSolution> construction_neighbourhood(IRoarNetProblem<TSolution> problem);
    IRoarNetSolution<TSolution> copy_solution(IRoarNetSolution<TSolution> solution);
    IRoarNetNeighborhood<TSolution> destruction_neighbourhood(IRoarNetProblem<TSolution> problem);
    IRoarNetSolution<TSolution> empty_solution(IRoarNetProblem<TSolution> problem);
    IRoarNetSolution<TSolution>? heuristic_solution(IRoarNetProblem<TSolution> problem);
    IRoarNetNeighborhood<TSolution> local_neighbourhood(IRoarNetProblem<TSolution> problem);
    double? lower_bound(IRoarNetSolution<TSolution> solution);
    double? lower_bound_increment(IRoarNetMove<TSolution> move, IRoarNetSolution<TSolution> solution);
    IEnumerable<IRoarNetMove<TSolution>> moves(IRoarNetNeighborhood<TSolution> neighbourhood, IRoarNetSolution<TSolution> solution);
    double? objective_value(IRoarNetSolution<TSolution> solution);
    double? objective_value_increment(IRoarNetMove<TSolution> move, IRoarNetSolution<TSolution> solution);
    IRoarNetMove<TSolution>? random_move(IRoarNetNeighborhood<TSolution> neighbourhood, IRoarNetSolution<TSolution> solution);
    IEnumerable<IRoarNetMove<TSolution>> random_moves_without_replacement(IRoarNetNeighborhood<TSolution> neighbourhood, IRoarNetSolution<TSolution> solution);
    IRoarNetSolution<TSolution> random_solution(IRoarNetProblem<TSolution> problem);
    IRoarNetSolution<TSolution> revert_move(IRoarNetMove<TSolution> move, IRoarNetSolution<TSolution> solution);
}

public interface IRoarNetMove<TG> : Move
{
    IRoarNetSolution<TG> Apply(IRoarNetSolution<TG> solution);
    double? LowerBoundIncrement(IRoarNetSolution<TG> solution);
    double? ObjectiveValueIncrement(IRoarNetSolution<TG> solution);
    IRoarNetSolution<TG> Revert(IRoarNetSolution<TG> solution);
}

public interface IRoarNetNeighborhood<TG> : Neighbourhood
{
    IEnumerable<IRoarNetMove<TG>> Moves(IRoarNetSolution<TG> solution);
    IRoarNetMove<TG>? RandomMove(IRoarNetSolution<TG> solution);
    IEnumerable<IRoarNetMove<TG>> RandomMoveWithOutReplacement(IRoarNetSolution<TG> solution);
}

public interface IRoarNetSolution<out TG> : Solution
{
    TG Genotype { get; }
    double? LowerBound { get; }
    double? ObjectiveValue { get; }
    IRoarNetSolution<TG> Copy();
};

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

public interface IRoarNetProblem<TG, out TS, out TP> : IRoarNetProblem<TG>
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
{
    TP Problem { get; }
    TS SearchSpace { get; }
}

/// <summary>
/// this is the only entry point needed 
/// </summary>
/// <typeparam name="TG"></typeparam>
/// <typeparam name="TS"></typeparam>
/// <typeparam name="TP"></typeparam>
/// <typeparam name="TM1"></typeparam>
/// <typeparam name="TM2"></typeparam>
/// <typeparam name="TM3"></typeparam>
/// <param name="problem"></param>
/// <param name="Evaluator"></param>
/// <param name="BoundsEvaluator"></param>
/// <param name="constructionNeighborhood"></param>
/// <param name="destructionNeighborhood"></param>
/// <param name="localNeighborhood"></param>
/// <param name="emptyCreator"></param>
/// <param name="randomCreator"></param>
/// <param name="heuristicCreator"></param>
/// <param name="rng"></param>
/// <param name="registry"></param>
public record RoarNetProblem<TG, TS, TP, TM1, TM2, TM3>(
    TP problem,
    IEvaluator<TG, TS, TP> Evaluator,
    IEvaluator<TG, TS, TP> BoundsEvaluator,
    INeighborhood<TG, TS, TP, TM1> constructionNeighborhood,
    INeighborhood<TG, TS, TP, TM2> destructionNeighborhood,
    INeighborhood<TG, TS, TP, TM3> localNeighborhood,
    ICreator<TG, TS, TP> emptyCreator,
    ICreator<TG, TS, TP> randomCreator,
    ICreator<TG, TS, TP> heuristicCreator,
    IRandomNumberGenerator rng,
    ExecutionInstanceRegistry registry) : IRoarNetProblem<TG, TS, TP>
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
{
    public TP Problem { get; } = problem;
    public TS SearchSpace { get; } = problem.SearchSpace;
    private readonly ICreatorInstance<TG, TS, TP> ec = registry.Resolve(emptyCreator);
    private readonly ICreatorInstance<TG, TS, TP> rc = registry.Resolve(randomCreator);
    private readonly ICreatorInstance<TG, TS, TP> hc = registry.Resolve(heuristicCreator);
    private readonly IEvaluatorInstance<TG, TS, TP> evaluatorInstance = registry.Resolve(Evaluator);
    private readonly IEvaluatorInstance<TG, TS, TP> boundsInstance = registry.Resolve(BoundsEvaluator);

    public IRoarNetNeighborhood<TG> ConstructionNeighbourhood() => new RoarNetNeighborhood<TG, TS, TP, TM1>(constructionNeighborhood, this, registry, rng);
    public IRoarNetNeighborhood<TG> DestructionNeighbourhood() => new RoarNetNeighborhood<TG, TS, TP, TM2>(destructionNeighborhood, this, registry, rng);
    public IRoarNetNeighborhood<TG> LocalNeighbourhood() => new RoarNetNeighborhood<TG, TS, TP, TM3>(localNeighborhood, this, registry, rng);
    public IRoarNetSolution<TG> EmptySolution() => new RoarNetSolution<TG>(ec.Create(1, rng, SearchSpace, Problem)[0], this);
    public IRoarNetSolution<TG> RandomSolution() => new RoarNetSolution<TG>(rc.Create(1, rng, SearchSpace, Problem)[0], this);
    public IRoarNetSolution<TG> HeuristicSolution() => new RoarNetSolution<TG>(hc.Create(1, rng, SearchSpace, Problem)[0], this);
    public double? LowerBound(TG genotype) => boundsInstance.Evaluate([genotype], rng, SearchSpace, Problem)[0].ObjectiveVector[0];

    public double? Objective(TG genotype) => evaluatorInstance.Evaluate([genotype], rng, SearchSpace, Problem)[0].ObjectiveVector[0];

    public RoarNetOperations<TG> GetOperations() => RoarNetOperations<TG>.Instance;
}

public readonly struct RoarNetSolution<TG>(TG genotype, IRoarNetProblem<TG> problem) : IRoarNetSolution<TG>
{
    public TG Genotype { get; } = genotype;
    private IRoarNetProblem<TG> Problem { get; } = problem;
    public double? LowerBound => Problem.LowerBound(Genotype);
    public double? ObjectiveValue => Problem.Objective(Genotype);
    public IRoarNetSolution<TG> Copy() => new RoarNetSolution<TG>(Genotype, Problem);
}

public readonly struct RoarNetMove<TG, TS, TP, TM>(TM move, RoarNetNeighborhood<TG, TS, TP, TM> neighborhood) : IRoarNetMove<TG>
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
{
    public IRoarNetSolution<TG> Apply(IRoarNetSolution<TG> solution) => neighborhood.ApplyMove(move, solution);
    public double? LowerBoundIncrement(IRoarNetSolution<TG> solution) => neighborhood.LowerBoundIncrement(move, solution);
    public double? ObjectiveValueIncrement(IRoarNetSolution<TG> solution) => neighborhood.ObjectiveValueIncrement(move, solution);
    public IRoarNetSolution<TG> Revert(IRoarNetSolution<TG> solution) => neighborhood.Revert(move, solution);
}

public readonly struct RoarNetNeighborhood<TG, TS, TP, TM>(
    INeighborhood<TG, TS, TP, TM> neighborhood,
    IRoarNetProblem<TG, TS, TP> problem,
    ExecutionInstanceRegistry registry,
    IRandomNumberGenerator rng)
    : IRoarNetNeighborhood<TG>
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
{
    private readonly INeighborhoodInstance<TG, TS, TP, TM> ni = registry.Resolve(neighborhood);

    private IRoarNetMove<TG> MakeMove(TM move)
    {
        return new RoarNetMove<TG, TS, TP, TM>(move, this);
    }

    public IRoarNetSolution<TG> ApplyMove(TM move, IRoarNetSolution<TG> solution)
    {
        return new RoarNetSolution<TG>(ni.ApplyMove(solution.Genotype, move, problem.SearchSpace, problem.Problem), problem);
    }

    public IEnumerable<IRoarNetMove<TG>> Moves(IRoarNetSolution<TG> solution)
    {
        return ni.Moves(solution.Genotype, rng, problem.SearchSpace, problem.Problem).Select(MakeMove);
    }

    public IRoarNetMove<TG>? RandomMove(IRoarNetSolution<TG> solution)
    {
        return ni.RandomMove(solution.Genotype, rng, problem.SearchSpace, problem.Problem, out var m) ? new RoarNetMove<TG, TS, TP, TM>(m, this) : null;
    }

    public IEnumerable<IRoarNetMove<TG>> RandomMoveWithOutReplacement(IRoarNetSolution<TG> solution)
    {
        return ni.Moves(solution.Genotype, rng, problem.SearchSpace, problem.Problem).Select(MakeMove);
    }

    public double? LowerBoundIncrement(TM move, IRoarNetSolution<TG> solution)
    {
        if (ni is IIncrementalBoundNeighborhoodInstance<TG, TS, TP, TM> bin)
            return bin.BoundIncrement(solution.Genotype, move, rng, problem.SearchSpace, problem.Problem)?[0];
        throw new NotSupportedException();
    }

    public double? ObjectiveValueIncrement(TM move, IRoarNetSolution<TG> solution)
    {
        if (ni is IIncrementalObjectiveNeighborhoodInstance<TG, TS, TP, TM> bin)
            return bin.EvaluateIncrement(solution.Genotype, move, rng, problem.SearchSpace, problem.Problem)?[0];
        throw new NotSupportedException();
    }

    public IRoarNetSolution<TG> Revert(TM move, IRoarNetSolution<TG> solution)
    {
        if (ni is IReversibleNeighborhoodInstance<TG, TS, TP, TM> bin)
            return new RoarNetSolution<TG>(bin.RevertMove(solution.Genotype, move, problem.SearchSpace, problem.Problem), problem);
        throw new NotSupportedException();
    }
}

public record RoarNetOperations<TG> : IRoarNetOperations<TG>
{
    public static RoarNetOperations<TG> Instance { get; } = new RoarNetOperations<TG>();
    private RoarNetOperations() { }

    #region typeless
    public Solution apply_move(Move move, Solution solution) => apply_move((IRoarNetMove<TG>)move, (IRoarNetSolution<TG>)solution);

    public Neighbourhood construction_neighbourhood(Problem problem) => construction_neighbourhood((IRoarNetProblem<TG>)problem);

    public Solution copy_solution(Solution solution) => copy_solution((IRoarNetSolution<TG>)solution);

    public Neighbourhood destruction_neighbourhood(Problem problem) => destruction_neighbourhood((IRoarNetProblem<TG>)problem);

    public Solution empty_solution(Problem problem) => empty_solution((IRoarNetProblem<TG>)problem);

    public Solution? heuristic_solution(Problem problem) => heuristic_solution((IRoarNetProblem<TG>)problem);

    public Neighbourhood local_neighbourhood(Problem problem) => local_neighbourhood((IRoarNetProblem<TG>)problem);

    public double? lower_bound(Solution solution) => lower_bound((IRoarNetSolution<TG>)solution);

    public double? lower_bound_increment(Move move, Solution solution) => lower_bound_increment((IRoarNetMove<TG>)move, (IRoarNetSolution<TG>)solution);

    public IEnumerable<Move> moves(Neighbourhood neighbourhood, Solution solution) => moves((IRoarNetNeighborhood<TG>)neighbourhood, (IRoarNetSolution<TG>)solution);

    public double? objective_value(Solution solution) => objective_value((IRoarNetSolution<TG>)solution);

    public double? objective_value_increment(Move move, Solution solution) => objective_value_increment((IRoarNetMove<TG>)move, (IRoarNetSolution<TG>)solution);

    public Move? random_move(Neighbourhood neighbourhood, Solution solution) => random_move((IRoarNetNeighborhood<TG>)neighbourhood, (IRoarNetSolution<TG>)solution);

    public IEnumerable<Move> random_moves_without_replacement(Neighbourhood neighbourhood, Solution solution) => random_moves_without_replacement((IRoarNetNeighborhood<TG>)neighbourhood, (IRoarNetSolution<TG>)solution);

    public Solution random_solution(Problem problem) => random_solution((IRoarNetProblem<TG>)problem);

    public Solution revert_move(Move move, Solution solution) => revert_move((IRoarNetMove<TG>)move, (IRoarNetSolution<TG>)solution);
    #endregion

    //from here typed operations

    public IRoarNetSolution<TG> apply_move(IRoarNetMove<TG> roarNetMove, IRoarNetSolution<TG> solution) => roarNetMove.Apply(solution);

    public IRoarNetNeighborhood<TG> construction_neighbourhood(IRoarNetProblem<TG> problem) => problem.ConstructionNeighbourhood();

    public IRoarNetSolution<TG> copy_solution(IRoarNetSolution<TG> solution) => solution.Copy();

    public IRoarNetNeighborhood<TG> destruction_neighbourhood(IRoarNetProblem<TG> problem) => problem.DestructionNeighbourhood();

    public IRoarNetSolution<TG> empty_solution(IRoarNetProblem<TG> problem) => problem.EmptySolution();

    public IRoarNetSolution<TG>? heuristic_solution(IRoarNetProblem<TG> problem) => problem.HeuristicSolution();

    public IRoarNetNeighborhood<TG> local_neighbourhood(IRoarNetProblem<TG> problem) => problem.LocalNeighbourhood();

    public double? lower_bound(IRoarNetSolution<TG> solution) => solution.LowerBound;

    public double? lower_bound_increment(IRoarNetMove<TG> move, IRoarNetSolution<TG> solution) => move.LowerBoundIncrement(solution);

    public IEnumerable<IRoarNetMove<TG>> moves(IRoarNetNeighborhood<TG> neighbourhood, IRoarNetSolution<TG> solution) => neighbourhood.Moves(solution);

    public double? objective_value(IRoarNetSolution<TG> solution) => solution.ObjectiveValue;

    public double? objective_value_increment(IRoarNetMove<TG> move, IRoarNetSolution<TG> solution) => move.ObjectiveValueIncrement(solution);

    public IRoarNetMove<TG>? random_move(IRoarNetNeighborhood<TG> neighbourhood, IRoarNetSolution<TG> solution) => neighbourhood.RandomMove(solution);

    public IEnumerable<IRoarNetMove<TG>> random_moves_without_replacement(IRoarNetNeighborhood<TG> neighbourhood, IRoarNetSolution<TG> solution) => neighbourhood.RandomMoveWithOutReplacement(solution);

    public IRoarNetSolution<TG> random_solution(IRoarNetProblem<TG> problem) => problem.RandomSolution();

    public IRoarNetSolution<TG> revert_move(IRoarNetMove<TG> move, IRoarNetSolution<TG> solution) => move.Revert(solution);
}
#endregion

#region Wrapping a ROAR-NET Operations to HeuristicLib
public sealed record RoarNetSearchSpace : ISearchSpace<Solution>
{
    public static RoarNetSearchSpace Instance { get; } = new();
    private RoarNetSearchSpace() { }
    public bool Contains(Solution genotype) => true;
}

public sealed class RoarNetProblem(Operations operations, Problem roarNetProblemInstance) : SingleSolutionProblem<Solution, RoarNetSearchSpace>(SingleObjective.Minimize, RoarNetSearchSpace.Instance)
{
    public Operations Operations { get; } = operations;
    public Problem RoarNetProblemInstance { get; } = roarNetProblemInstance;

    public override ObjectiveVector Evaluate(Solution solution, IRandomNumberGenerator random) => Operations.objective_value(solution)
                                                                                                  ?? throw new InvalidOperationException("ROAR-NET solution is not objectively evaluable.");
}

public sealed record RoarNetRandomSolutionCreator : StatelessCreator<Solution, RoarNetSearchSpace, RoarNetProblem>
{
    public override IReadOnlyList<Solution> Create(int count, IRandomNumberGenerator random, RoarNetSearchSpace searchSpace, RoarNetProblem problem)
        => Enumerable.Range(0, count).Select(_ => problem.Operations.random_solution(problem.RoarNetProblemInstance)).ToArray();
}

public sealed record RoarNetHeuristicSolutionCreator : StatelessCreator<Solution, RoarNetSearchSpace, RoarNetProblem>
{
    public override IReadOnlyList<Solution> Create(int count, IRandomNumberGenerator random, RoarNetSearchSpace searchSpace, RoarNetProblem problem)
        => Enumerable.Range(0, count).Select(_ => problem.Operations.heuristic_solution(problem.RoarNetProblemInstance) ?? throw new NotSupportedException()).ToArray();
}

public sealed record RoarNetEmptySolutionCreator : StatelessCreator<Solution, RoarNetSearchSpace, RoarNetProblem>
{
    public override IReadOnlyList<Solution> Create(int count, IRandomNumberGenerator random, RoarNetSearchSpace searchSpace, RoarNetProblem problem)
        => Enumerable.Range(0, count).Select(_ => problem.Operations.empty_solution(problem.RoarNetProblemInstance)).ToArray();
}

public abstract record RoarNetNeighborhood(RoarNetProblem problem) : FullFeatureNeighborhood<Solution, RoarNetSearchSpace, RoarNetProblem, Move, RoarNetNeighborhood.State>
{
    public record State(Neighbourhood Neighbourhood);

    protected override State CreateInitialState()
        => new(GetNeighborhood());

    protected abstract Neighbourhood GetNeighborhood();

    protected override IEnumerable<Move> Moves(Solution genotype, State executionState, IRandomNumberGenerator random, RoarNetSearchSpace searchSpace, RoarNetProblem problem)
        => problem.Operations.moves(executionState.Neighbourhood, genotype);

    protected override bool RandomMove(Solution genotype, State executionState, IRandomNumberGenerator random, RoarNetSearchSpace searchSpace, RoarNetProblem problem, [MaybeNullWhen(false)] out Move move)
    {
        move = problem.Operations.random_move(executionState.Neighbourhood, genotype);
        return move != null;
    }

    protected override Solution ApplyMove(Solution genotype, Move move, State executionState, RoarNetSearchSpace searchSpace, RoarNetProblem problem)
        => problem.Operations.apply_move(move, genotype);

    protected override Solution RevertMove(Solution genotype, Move move, State executionState, RoarNetSearchSpace searchSpace, RoarNetProblem problem)
        => problem.Operations.revert_move(move, genotype);

    protected override ObjectiveVector? EvaluateIncrement(Solution genotype, Move move, State executionState, IRandomNumberGenerator random, RoarNetSearchSpace searchSpace, RoarNetProblem problem)
        => problem.Operations.objective_value_increment(move, genotype);

    protected override ObjectiveVector? BoundIncrement(Solution genotype, Move move, State executionState, IRandomNumberGenerator random, RoarNetSearchSpace searchSpace, RoarNetProblem problem)
        => problem.Operations.lower_bound_increment(move, genotype);
}

public record RoarNetLocalNeighborhood(RoarNetProblem problem) : RoarNetNeighborhood(problem)
{
    protected override Neighbourhood GetNeighborhood() => problem.Operations.local_neighbourhood(problem.RoarNetProblemInstance);
}

public record RoarNetConstructionNeighborhood(RoarNetProblem problem) : RoarNetNeighborhood(problem)
{
    protected override Neighbourhood GetNeighborhood() => problem.Operations.construction_neighbourhood(problem.RoarNetProblemInstance);
}

public record RoarNetDestructionNeighborhood(RoarNetProblem problem) : RoarNetNeighborhood(problem)
{
    protected override Neighbourhood GetNeighborhood() => problem.Operations.destruction_neighbourhood(problem.RoarNetProblemInstance);
}
#endregion
