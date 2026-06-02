// ReSharper disable InconsistentNaming

#pragma warning disable S101
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.Partial;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.APIs.RoarNet;

/// <summary>
/// https://github.com/roar-net/roar-net-api-spec/blob/main/src/types/Move.md
/// </summary>
public interface Move;

public abstract record HLibRoarMove : Move;

public sealed record HLibRoarMove<TMove>(
    TMove Value,
    object Neighborhood
) : HLibRoarMove;

public sealed class HLibRoarSolution<TGenotype>(TGenotype genotype) : Solution
{
    public TGenotype Genotype { get; } = genotype;
}

public abstract record HLibRoarNeighbourhood : Neighbourhood;

public sealed record HLibRoarNeighbourhood<TNeighborhood>(TNeighborhood Value) : HLibRoarNeighbourhood;

public sealed record HLibRoarProblem<TGenotype, TSearchSpace, TProblem>(
    TProblem InnerProblem) : Problem, IProblem<TGenotype, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
    public TSearchSpace SearchSpace => InnerProblem.SearchSpace;
    public Objective Objective => InnerProblem.Objective;
    public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, IRandomNumberGenerator random) => InnerProblem.Evaluate(genotypes, random);
}

public sealed record HLibRoarOperations<
    TGenotype,
    TSearchSpace,
    TProblem,
    TConstructionNeighborhood,
    TConstructionMove,
    TLocalNeighborhood,
    TLocalMove>(
    TProblem Problem,
    TConstructionNeighborhood ConstructionNeighborhood,
    TLocalNeighborhood LocalNeighborhood,
    IRandomNumberGenerator Random, ICreator<TGenotype, TSearchSpace, TProblem>? creator)
    : BaseOperations<
        HLibRoarSolution<TGenotype>,
        HLibRoarMove,
        HLibRoarNeighbourhood,
        HLibRoarProblem<TGenotype, TSearchSpace, TProblem>>
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
    where TConstructionNeighborhood : INeighborhood<TGenotype, TSearchSpace, TProblem, TConstructionMove>
    where TLocalNeighborhood : INeighborhood<TGenotype, TSearchSpace, TProblem, TLocalMove>
{
    public override HLibRoarNeighbourhood construction_neighbourhood(
        HLibRoarProblem<TGenotype, TSearchSpace, TProblem> problem)
        => new HLibRoarNeighbourhood<TConstructionNeighborhood>(ConstructionNeighborhood);

    public override HLibRoarNeighbourhood local_neighbourhood(
        HLibRoarProblem<TGenotype, TSearchSpace, TProblem> problem)
        => new HLibRoarNeighbourhood<TLocalNeighborhood>(LocalNeighborhood);

    public override HLibRoarSolution<TGenotype> apply_move(
        HLibRoarMove move,
        HLibRoarSolution<TGenotype> solution)
        => move switch
        {
            HLibRoarMove<TConstructionMove> m
                when ReferenceEquals(m.Neighborhood, ConstructionNeighborhood)
                => new HLibRoarSolution<TGenotype>(ConstructionNeighborhood.ApplyMove(
                    solution.Genotype,
                    m.Value,
                    Problem.SearchSpace,
                    Problem)),

            HLibRoarMove<TLocalMove> m
                when ReferenceEquals(m.Neighborhood, LocalNeighborhood)
                => new HLibRoarSolution<TGenotype>(LocalNeighborhood.ApplyMove(
                    solution.Genotype,
                    m.Value,
                    Problem.SearchSpace,
                    Problem)),

            _ => throw new ArgumentException("Move does not belong to this operation set.", nameof(move))
        };

    public override IEnumerable<HLibRoarMove> moves(
        HLibRoarNeighbourhood neighbourhood,
        HLibRoarSolution<TGenotype> solution)
        => neighbourhood switch
        {
            HLibRoarNeighbourhood<TConstructionNeighborhood> =>
                ConstructionNeighborhood
                    .Moves(solution.Genotype, Random, Problem.SearchSpace, Problem)
                    .Select(m => new HLibRoarMove<TConstructionMove>(m, ConstructionNeighborhood)),

            HLibRoarNeighbourhood<TLocalNeighborhood> =>
                LocalNeighborhood
                    .Moves(solution.Genotype, Random, Problem.SearchSpace, Problem)
                    .Select(m => new HLibRoarMove<TLocalMove>(m, LocalNeighborhood)),

            _ => throw new ArgumentException("Unknown neighbourhood.", nameof(neighbourhood))
        };

    public override HLibRoarMove? random_move(
        HLibRoarNeighbourhood neighbourhood,
        HLibRoarSolution<TGenotype> solution)
    {
        // Replace with whatever RNG you actually want to inject.
        return neighbourhood switch
        {
            HLibRoarNeighbourhood<TConstructionNeighborhood>
                when ConstructionNeighborhood.RandomMove(
                    solution.Genotype, Random, Problem.SearchSpace, Problem, out var move)
                => new HLibRoarMove<TConstructionMove>(move, ConstructionNeighborhood),

            HLibRoarNeighbourhood<TLocalNeighborhood>
                when LocalNeighborhood.RandomMove(
                    solution.Genotype, Random, Problem.SearchSpace, Problem, out var move)
                => new HLibRoarMove<TLocalMove>(move, LocalNeighborhood),

            _ => null
        };
    }

    public override double? objective_value(HLibRoarSolution<TGenotype> solution)
        => Problem.Evaluate([solution.Genotype], Random)[0][0];

    public override double? lower_bound(HLibRoarSolution<TGenotype> solution)
        => Problem is IBoundedProblem<TGenotype, TSearchSpace> partial
            ? partial.Bound(solution.Genotype, Random)[0]
            : objective_value(solution);

    public override HLibRoarSolution<TGenotype> copy_solution(
        HLibRoarSolution<TGenotype> solution)
        => new(solution.Genotype);

    public override HLibRoarSolution<TGenotype> empty_solution(
        HLibRoarProblem<TGenotype, TSearchSpace, TProblem> problem)
        => throw new NotSupportedException("No generic empty-solution constructor is available.");

    public override HLibRoarSolution<TGenotype> heuristic_solution(
        HLibRoarProblem<TGenotype, TSearchSpace, TProblem> problem)
        => throw new NotSupportedException("Use an HLib creator-backed adapter for random complete solutions.");;

    public override HLibRoarSolution<TGenotype> random_solution(
        HLibRoarProblem<TGenotype, TSearchSpace, TProblem> problem)
        => throw new NotSupportedException("Use an HLib creator-backed adapter for random complete solutions.");

    public override HLibRoarNeighbourhood destruction_neighbourhood(
        HLibRoarProblem<TGenotype, TSearchSpace, TProblem> problem)
        => throw new NotSupportedException("No destruction neighbourhood was supplied.");

    public override double? objective_value_increment(
        HLibRoarMove move,
        HLibRoarSolution<TGenotype> solution)
        => move switch
        {
            HLibRoarMove<TLocalMove> m
                when LocalNeighborhood is IIncrementalObjectiveNeighborhood<
                    TGenotype, TSearchSpace, TProblem, TLocalMove> inc
                => inc.EvaluateIncrement(solution.Genotype, m.Value,
                    Random, Problem.SearchSpace, Problem)?[0],

            HLibRoarMove<TConstructionMove> m
                when ConstructionNeighborhood is IIncrementalObjectiveNeighborhood<
                    TGenotype, TSearchSpace, TProblem, TConstructionMove> inc
                => inc.EvaluateIncrement(solution.Genotype, m.Value,
                    Random, Problem.SearchSpace, Problem)?[0],

            _ => null
        };

    public override double? lower_bound_increment(
        HLibRoarMove move,
        HLibRoarSolution<TGenotype> solution)
        => move switch
        {
            HLibRoarMove<TLocalMove> m
                when LocalNeighborhood is IIncrementalBoundNeighborhood<
                    TGenotype, TSearchSpace, TProblem, TLocalMove> inc
                => inc.BoundIncrement(solution.Genotype, m.Value,
                    Random, Problem.SearchSpace, Problem)?[0],

            HLibRoarMove<TConstructionMove> m
                when ConstructionNeighborhood is IIncrementalBoundNeighborhood<
                    TGenotype, TSearchSpace, TProblem, TConstructionMove> inc
                => inc.BoundIncrement(solution.Genotype, m.Value,
                    Random, Problem.SearchSpace, Problem)?[0],

            _ => null
        };

    public override IEnumerable<HLibRoarMove> random_moves_without_replacement(
        HLibRoarNeighbourhood neighbourhood,
        HLibRoarSolution<TGenotype> solution)
        => moves(neighbourhood, solution);

    public override HLibRoarSolution<TGenotype> revert_move(
        HLibRoarMove move,
        HLibRoarSolution<TGenotype> solution)
        => throw new NotSupportedException("Generic ROAR-NET move reversion is not available yet.");
}
