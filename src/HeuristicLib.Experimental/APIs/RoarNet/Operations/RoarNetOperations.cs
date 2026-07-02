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

public interface IRoarNetMove<TCandidate> : Move
{
    IRoarNetSolution<TCandidate> Apply(IRoarNetSolution<TCandidate> solution);
    double? LowerBoundIncrement(IRoarNetSolution<TCandidate> solution);
    double? ObjectiveValueIncrement(IRoarNetSolution<TCandidate> solution);
    IRoarNetSolution<TCandidate> Revert(IRoarNetSolution<TCandidate> solution);
}

public interface IRoarNetNeighborhood<TCandidate> : Neighbourhood
{
    IEnumerable<IRoarNetMove<TCandidate>> Moves(IRoarNetSolution<TCandidate> solution);
    IRoarNetMove<TCandidate>? RandomMove(IRoarNetSolution<TCandidate> solution);
    IEnumerable<IRoarNetMove<TCandidate>> RandomMoveWithOutReplacement(IRoarNetSolution<TCandidate> solution);
}

public interface IRoarNetSolution<out TCandidate> : Solution
{
    TCandidate Candidate { get; }
    double? LowerBound { get; }
    double? ObjectiveValue { get; }
    IRoarNetSolution<TCandidate> Copy();
};

public interface IRoarNetProblem<TCandidate> : Problem
{
    IRoarNetNeighborhood<TCandidate> ConstructionNeighbourhood();
    IRoarNetNeighborhood<TCandidate> DestructionNeighbourhood();
    IRoarNetNeighborhood<TCandidate> LocalNeighbourhood();

    IRoarNetSolution<TCandidate> EmptySolution();
    IRoarNetSolution<TCandidate> RandomSolution();
    IRoarNetSolution<TCandidate>? HeuristicSolution();
    double? LowerBound(TCandidate candidate);
    double? Objective(TCandidate candidate);
}

public interface IRoarNetProblem<TCandidate, out TSearchSpace, out TProblem> : IRoarNetProblem<TCandidate>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    TProblem Problem { get; }
    TSearchSpace SearchSpace { get; }
}

/// <summary>
/// this is the only entry point needed 
/// </summary>
/// <typeparam name="TCandidate"></typeparam>
/// <typeparam name="TSearchSpace"></typeparam>
/// <typeparam name="TProblem"></typeparam>
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
public record RoarNetProblem<TCandidate, TSearchSpace, TProblem, TM1, TM2, TM3>(
    TProblem problem,
    IEvaluator<TCandidate, TSearchSpace, TProblem> Evaluator,
    IEvaluator<TCandidate, TSearchSpace, TProblem> BoundsEvaluator,
    INeighborhood<TCandidate, TSearchSpace, TProblem, TM1> constructionNeighborhood,
    INeighborhood<TCandidate, TSearchSpace, TProblem, TM2> destructionNeighborhood,
    INeighborhood<TCandidate, TSearchSpace, TProblem, TM3> localNeighborhood,
    ICreator<TCandidate, TSearchSpace, TProblem> emptyCreator,
    ICreator<TCandidate, TSearchSpace, TProblem> randomCreator,
    ICreator<TCandidate, TSearchSpace, TProblem> heuristicCreator,
    IRandomNumberGenerator rng,
    ExecutionInstanceRegistry registry) : IRoarNetProblem<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public TProblem Problem { get; } = problem;
    public TSearchSpace SearchSpace { get; } = problem.SearchSpace;
    private readonly ICreatorInstance<TCandidate, TSearchSpace, TProblem> ec = registry.Resolve(emptyCreator);
    private readonly ICreatorInstance<TCandidate, TSearchSpace, TProblem> rc = registry.Resolve(randomCreator);
    private readonly ICreatorInstance<TCandidate, TSearchSpace, TProblem> hc = registry.Resolve(heuristicCreator);
    private readonly IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> evaluatorInstance = registry.Resolve(Evaluator);
    private readonly IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> boundsInstance = registry.Resolve(BoundsEvaluator);

    public IRoarNetNeighborhood<TCandidate> ConstructionNeighbourhood() => new RoarNetNeighborhood<TCandidate, TSearchSpace, TProblem, TM1>(constructionNeighborhood, this, registry, rng);
    public IRoarNetNeighborhood<TCandidate> DestructionNeighbourhood() => new RoarNetNeighborhood<TCandidate, TSearchSpace, TProblem, TM2>(destructionNeighborhood, this, registry, rng);
    public IRoarNetNeighborhood<TCandidate> LocalNeighbourhood() => new RoarNetNeighborhood<TCandidate, TSearchSpace, TProblem, TM3>(localNeighborhood, this, registry, rng);
    public IRoarNetSolution<TCandidate> EmptySolution() => new RoarNetSolution<TCandidate>(ec.Create(1, rng, SearchSpace, Problem)[0], this);
    public IRoarNetSolution<TCandidate> RandomSolution() => new RoarNetSolution<TCandidate>(rc.Create(1, rng, SearchSpace, Problem)[0], this);
    public IRoarNetSolution<TCandidate> HeuristicSolution() => new RoarNetSolution<TCandidate>(hc.Create(1, rng, SearchSpace, Problem)[0], this);
    public double? LowerBound(TCandidate candidate) => boundsInstance.Evaluate([candidate], rng, SearchSpace, Problem)[0][0];

    public double? Objective(TCandidate candidate) => evaluatorInstance.Evaluate([candidate], rng, SearchSpace, Problem)[0][0];

    public RoarNetOperations<TCandidate> GetOperations() => RoarNetOperations<TCandidate>.Instance;
}

public readonly struct RoarNetSolution<TCandidate>(TCandidate candidate, IRoarNetProblem<TCandidate> problem) : IRoarNetSolution<TCandidate>
{
    public TCandidate Candidate { get; } = candidate;
    private IRoarNetProblem<TCandidate> Problem { get; } = problem;
    public double? LowerBound => Problem.LowerBound(Candidate);
    public double? ObjectiveValue => Problem.Objective(Candidate);
    public IRoarNetSolution<TCandidate> Copy() => new RoarNetSolution<TCandidate>(Candidate, Problem);
}

public readonly struct RoarNetMove<TCandidate, TSearchSpace, TProblem, TM>(TM move, RoarNetNeighborhood<TCandidate, TSearchSpace, TProblem, TM> neighborhood) : IRoarNetMove<TCandidate>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public IRoarNetSolution<TCandidate> Apply(IRoarNetSolution<TCandidate> solution) => neighborhood.ApplyMove(move, solution);
    public double? LowerBoundIncrement(IRoarNetSolution<TCandidate> solution) => neighborhood.LowerBoundIncrement(move, solution);
    public double? ObjectiveValueIncrement(IRoarNetSolution<TCandidate> solution) => neighborhood.ObjectiveValueIncrement(move, solution);
    public IRoarNetSolution<TCandidate> Revert(IRoarNetSolution<TCandidate> solution) => neighborhood.Revert(move, solution);
}

public readonly struct RoarNetNeighborhood<TCandidate, TSearchSpace, TProblem, TM>(
    INeighborhood<TCandidate, TSearchSpace, TProblem, TM> neighborhood,
    IRoarNetProblem<TCandidate, TSearchSpace, TProblem> problem,
    ExecutionInstanceRegistry registry,
    IRandomNumberGenerator rng)
    : IRoarNetNeighborhood<TCandidate>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    private readonly INeighborhoodInstance<TCandidate, TSearchSpace, TProblem, TM> ni = registry.Resolve(neighborhood);

    private IRoarNetMove<TCandidate> MakeMove(TM move)
    {
        return new RoarNetMove<TCandidate, TSearchSpace, TProblem, TM>(move, this);
    }

    public IRoarNetSolution<TCandidate> ApplyMove(TM move, IRoarNetSolution<TCandidate> solution)
    {
        return new RoarNetSolution<TCandidate>(ni.ApplyMove(solution.Candidate, move, problem.SearchSpace, problem.Problem), problem);
    }

    public IEnumerable<IRoarNetMove<TCandidate>> Moves(IRoarNetSolution<TCandidate> solution)
    {
        return ni.Moves(solution.Candidate, rng, problem.SearchSpace, problem.Problem).Select(MakeMove);
    }

    public IRoarNetMove<TCandidate>? RandomMove(IRoarNetSolution<TCandidate> solution)
    {
        return ni.RandomMove(solution.Candidate, rng, problem.SearchSpace, problem.Problem, out var m) ? new RoarNetMove<TCandidate, TSearchSpace, TProblem, TM>(m, this) : null;
    }

    public IEnumerable<IRoarNetMove<TCandidate>> RandomMoveWithOutReplacement(IRoarNetSolution<TCandidate> solution)
    {
        return ni.Moves(solution.Candidate, rng, problem.SearchSpace, problem.Problem).Select(MakeMove);
    }

    public double? LowerBoundIncrement(TM move, IRoarNetSolution<TCandidate> solution)
    {
        if (ni is IIncrementalBoundNeighborhoodInstance<TCandidate, TSearchSpace, TProblem, TM> bin)
            return bin.BoundIncrement(solution.Candidate, move, rng, problem.SearchSpace, problem.Problem)?[0];
        throw new NotSupportedException();
    }

    public double? ObjectiveValueIncrement(TM move, IRoarNetSolution<TCandidate> solution)
    {
        if (ni is IIncrementalObjectiveNeighborhoodInstance<TCandidate, TSearchSpace, TProblem, TM> bin)
            return bin.EvaluateIncrement(solution.Candidate, move, rng, problem.SearchSpace, problem.Problem)?[0];
        throw new NotSupportedException();
    }

    public IRoarNetSolution<TCandidate> Revert(TM move, IRoarNetSolution<TCandidate> solution)
    {
        if (ni is IReversibleNeighborhoodInstance<TCandidate, TSearchSpace, TProblem, TM> bin)
            return new RoarNetSolution<TCandidate>(bin.RevertMove(solution.Candidate, move, problem.SearchSpace, problem.Problem), problem);
        throw new NotSupportedException();
    }
}

public record RoarNetOperations<TCandidate> : IRoarNetOperations<TCandidate>
{
    public static RoarNetOperations<TCandidate> Instance { get; } = new RoarNetOperations<TCandidate>();
    private RoarNetOperations() { }

    #region typeless
    public Solution apply_move(Move move, Solution solution) => apply_move((IRoarNetMove<TCandidate>)move, (IRoarNetSolution<TCandidate>)solution);

    public Neighbourhood construction_neighbourhood(Problem problem) => construction_neighbourhood((IRoarNetProblem<TCandidate>)problem);

    public Solution copy_solution(Solution solution) => copy_solution((IRoarNetSolution<TCandidate>)solution);

    public Neighbourhood destruction_neighbourhood(Problem problem) => destruction_neighbourhood((IRoarNetProblem<TCandidate>)problem);

    public Solution empty_solution(Problem problem) => empty_solution((IRoarNetProblem<TCandidate>)problem);

    public Solution? heuristic_solution(Problem problem) => heuristic_solution((IRoarNetProblem<TCandidate>)problem);

    public Neighbourhood local_neighbourhood(Problem problem) => local_neighbourhood((IRoarNetProblem<TCandidate>)problem);

    public double? lower_bound(Solution solution) => lower_bound((IRoarNetSolution<TCandidate>)solution);

    public double? lower_bound_increment(Move move, Solution solution) => lower_bound_increment((IRoarNetMove<TCandidate>)move, (IRoarNetSolution<TCandidate>)solution);

    public IEnumerable<Move> moves(Neighbourhood neighbourhood, Solution solution) => moves((IRoarNetNeighborhood<TCandidate>)neighbourhood, (IRoarNetSolution<TCandidate>)solution);

    public double? objective_value(Solution solution) => objective_value((IRoarNetSolution<TCandidate>)solution);

    public double? objective_value_increment(Move move, Solution solution) => objective_value_increment((IRoarNetMove<TCandidate>)move, (IRoarNetSolution<TCandidate>)solution);

    public Move? random_move(Neighbourhood neighbourhood, Solution solution) => random_move((IRoarNetNeighborhood<TCandidate>)neighbourhood, (IRoarNetSolution<TCandidate>)solution);

    public IEnumerable<Move> random_moves_without_replacement(Neighbourhood neighbourhood, Solution solution) => random_moves_without_replacement((IRoarNetNeighborhood<TCandidate>)neighbourhood, (IRoarNetSolution<TCandidate>)solution);

    public Solution random_solution(Problem problem) => random_solution((IRoarNetProblem<TCandidate>)problem);

    public Solution revert_move(Move move, Solution solution) => revert_move((IRoarNetMove<TCandidate>)move, (IRoarNetSolution<TCandidate>)solution);
    #endregion

    //from here typed operations

    public IRoarNetSolution<TCandidate> apply_move(IRoarNetMove<TCandidate> roarNetMove, IRoarNetSolution<TCandidate> solution) => roarNetMove.Apply(solution);

    public IRoarNetNeighborhood<TCandidate> construction_neighbourhood(IRoarNetProblem<TCandidate> problem) => problem.ConstructionNeighbourhood();

    public IRoarNetSolution<TCandidate> copy_solution(IRoarNetSolution<TCandidate> solution) => solution.Copy();

    public IRoarNetNeighborhood<TCandidate> destruction_neighbourhood(IRoarNetProblem<TCandidate> problem) => problem.DestructionNeighbourhood();

    public IRoarNetSolution<TCandidate> empty_solution(IRoarNetProblem<TCandidate> problem) => problem.EmptySolution();

    public IRoarNetSolution<TCandidate>? heuristic_solution(IRoarNetProblem<TCandidate> problem) => problem.HeuristicSolution();

    public IRoarNetNeighborhood<TCandidate> local_neighbourhood(IRoarNetProblem<TCandidate> problem) => problem.LocalNeighbourhood();

    public double? lower_bound(IRoarNetSolution<TCandidate> solution) => solution.LowerBound;

    public double? lower_bound_increment(IRoarNetMove<TCandidate> move, IRoarNetSolution<TCandidate> solution) => move.LowerBoundIncrement(solution);

    public IEnumerable<IRoarNetMove<TCandidate>> moves(IRoarNetNeighborhood<TCandidate> neighbourhood, IRoarNetSolution<TCandidate> solution) => neighbourhood.Moves(solution);

    public double? objective_value(IRoarNetSolution<TCandidate> solution) => solution.ObjectiveValue;

    public double? objective_value_increment(IRoarNetMove<TCandidate> move, IRoarNetSolution<TCandidate> solution) => move.ObjectiveValueIncrement(solution);

    public IRoarNetMove<TCandidate>? random_move(IRoarNetNeighborhood<TCandidate> neighbourhood, IRoarNetSolution<TCandidate> solution) => neighbourhood.RandomMove(solution);

    public IEnumerable<IRoarNetMove<TCandidate>> random_moves_without_replacement(IRoarNetNeighborhood<TCandidate> neighbourhood, IRoarNetSolution<TCandidate> solution) => neighbourhood.RandomMoveWithOutReplacement(solution);

    public IRoarNetSolution<TCandidate> random_solution(IRoarNetProblem<TCandidate> problem) => problem.RandomSolution();

    public IRoarNetSolution<TCandidate> revert_move(IRoarNetMove<TCandidate> move, IRoarNetSolution<TCandidate> solution) => move.Revert(solution);
}
#endregion

#region Wrapping a ROAR-NET Operations to HeuristicLib
public sealed record RoarNetSearchSpace : ISearchSpace<Solution>
{
    public static RoarNetSearchSpace Instance { get; } = new();
    private RoarNetSearchSpace() { }
    public bool Contains(Solution candidate) => true;
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

    protected override IEnumerable<Move> Moves(Solution candidate, State executionState, IRandomNumberGenerator random, RoarNetSearchSpace searchSpace, RoarNetProblem problem)
        => problem.Operations.moves(executionState.Neighbourhood, candidate);

    protected override bool RandomMove(Solution candidate, State executionState, IRandomNumberGenerator random, RoarNetSearchSpace searchSpace, RoarNetProblem problem, [MaybeNullWhen(false)] out Move move)
    {
        move = problem.Operations.random_move(executionState.Neighbourhood, candidate);
        return move != null;
    }

    protected override Solution ApplyMove(Solution candidate, Move move, State executionState, RoarNetSearchSpace searchSpace, RoarNetProblem problem)
        => problem.Operations.apply_move(move, candidate);

    protected override Solution RevertMove(Solution candidate, Move move, State executionState, RoarNetSearchSpace searchSpace, RoarNetProblem problem)
        => problem.Operations.revert_move(move, candidate);

    protected override ObjectiveVector? EvaluateIncrement(Solution candidate, Move move, State executionState, IRandomNumberGenerator random, RoarNetSearchSpace searchSpace, RoarNetProblem problem)
        => problem.Operations.objective_value_increment(move, candidate);

    protected override ObjectiveVector? BoundIncrement(Solution candidate, Move move, State executionState, IRandomNumberGenerator random, RoarNetSearchSpace searchSpace, RoarNetProblem problem)
        => problem.Operations.lower_bound_increment(move, candidate);
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
