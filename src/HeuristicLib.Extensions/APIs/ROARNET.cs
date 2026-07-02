// ReSharper disable InconsistentNaming

using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

#pragma warning disable S101

namespace HEAL.HeuristicLib.RoarNetApi;

/// <summary>
/// https://github.com/roar-net/roar-net-api-spec/blob/main/src/types/Move.md
/// </summary>
public interface Move;

/// <summary>
/// https://github.com/roar-net/roar-net-api-spec/blob/main/src/types/Neighbourhood.md
/// </summary>
public interface Neighbourhood;

/// <summary>
/// https://github.com/roar-net/roar-net-api-spec/blob/main/src/types/Problem.md
/// </summary>
public interface Problem;

/// <summary>
/// https://github.com/roar-net/roar-net-api-spec/blob/main/src/types/Solution.md
/// </summary>
public interface Solution;

public interface Operations
{
  public Solution apply_move(Move move, Solution solution);
  public Neighbourhood construction_neighbourhood(Problem problem);
  public Solution copy_solution(Solution solution);
  public Neighbourhood destruction_neighbourhood(Problem problem);
  public Solution empty_solution(Problem problem);
  public Solution? heuristic_solution(Problem problem);
  public Neighbourhood local_neighbourhood(Problem problem);
  public double? lower_bound(Solution solution);
  public double? lower_bound_increment(Move move, Solution solution);
  public IEnumerable<Move> moves(Neighbourhood neighbourhood, Solution solution);
  public double? objective_value(Solution solution);
  public double? objective_value_increment(Move move, Solution solution);
  public Move? random_move(Neighbourhood neighbourhood, Solution solution);
  public IEnumerable<Move> random_moves_without_replacement(Neighbourhood neighbourhood, Solution solution);
  public Solution random_solution(Problem problem);
  public Solution revert_move(Move move, Solution solution);
}

public interface Operations<TSolution, TMove, TNeighbourhood, in TProblem> : Operations
  where TSolution : Solution
  where TMove : Move
  where TNeighbourhood : Neighbourhood
  where TProblem : Problem
{
  public TSolution apply_move(TMove move, TSolution solution);
  public TNeighbourhood construction_neighbourhood(TProblem problem);
  public TSolution copy_solution(TSolution solution);
  public TNeighbourhood destruction_neighbourhood(TProblem problem);
  public TSolution empty_solution(TProblem problem);
  public TSolution? heuristic_solution(TProblem problem);
  public TNeighbourhood local_neighbourhood(TProblem problem);
  public double? lower_bound(TSolution solution);
  public double? lower_bound_increment(TMove move, TSolution solution);
  public IEnumerable<TMove> moves(TNeighbourhood neighbourhood, TSolution solution);
  public double? objective_value(TSolution solution);
  public double? objective_value_increment(TMove move, TSolution solution);
  public TMove? random_move(TNeighbourhood neighbourhood, TSolution solution);
  public IEnumerable<TMove> random_moves_without_replacement(TNeighbourhood neighbourhood, TSolution solution);
  public TSolution random_solution(TProblem problem);
  public TSolution revert_move(TMove move, TSolution solution);
}

public abstract record BaseOperations<TSolution, TMove, TNeighbourhood, TProblem> : Operations<TSolution, TMove, TNeighbourhood, TProblem>
  where TSolution : Solution
  where TMove : Move
  where TNeighbourhood : Neighbourhood
  where TProblem : Problem
{
  public abstract TSolution apply_move(TMove move, TSolution solution);
  public abstract TNeighbourhood construction_neighbourhood(TProblem problem);
  public abstract TSolution copy_solution(TSolution solution);
  public abstract TNeighbourhood destruction_neighbourhood(TProblem problem);
  public abstract TSolution empty_solution(TProblem problem);
  public abstract TSolution? heuristic_solution(TProblem problem);
  public abstract TNeighbourhood local_neighbourhood(TProblem problem);
  public abstract double? lower_bound(TSolution solution);
  public abstract double? lower_bound_increment(TMove move, TSolution solution);
  public abstract IEnumerable<TMove> moves(TNeighbourhood neighbourhood, TSolution solution);
  public abstract double? objective_value(TSolution solution);
  public abstract double? objective_value_increment(TMove move, TSolution solution);
  public abstract TMove? random_move(TNeighbourhood neighbourhood, TSolution solution);
  public abstract IEnumerable<TMove> random_moves_without_replacement(TNeighbourhood neighbourhood, TSolution solution);
  public abstract TSolution random_solution(TProblem problem);
  public abstract TSolution revert_move(TMove move, TSolution solution);

  #region ROAR-NET "type-reduced" wrappers
  public Solution apply_move(Move move, Solution solution) => apply_move((TMove)move, (TSolution)solution);

  public Neighbourhood construction_neighbourhood(Problem problem) => construction_neighbourhood((TProblem)problem);

  public Solution copy_solution(Solution solution) => copy_solution((TSolution)solution);

  public Neighbourhood destruction_neighbourhood(Problem problem) => destruction_neighbourhood((TProblem)problem);

  public Solution empty_solution(Problem problem) => empty_solution((TProblem)problem);

  public Solution? heuristic_solution(Problem problem) => heuristic_solution((TProblem)problem);

  public Neighbourhood local_neighbourhood(Problem problem) => local_neighbourhood((TProblem)problem);

  public double? lower_bound(Solution solution) => lower_bound((TSolution)solution);

  public double? lower_bound_increment(Move move, Solution solution) => lower_bound_increment((TMove)move, (TSolution)solution);

  public IEnumerable<Move> moves(Neighbourhood neighbourhood, Solution solution) => moves((TNeighbourhood)neighbourhood, (TSolution)solution).Cast<Move>();

  public double? objective_value(Solution solution) => objective_value((TSolution)solution);

  public double? objective_value_increment(Move move, Solution solution) => objective_value_increment((TMove)move, (TSolution)solution);

  public Move? random_move(Neighbourhood neighbourhood, Solution solution) => random_move((TNeighbourhood)neighbourhood, (TSolution)solution);

  public IEnumerable<Move> random_moves_without_replacement(Neighbourhood neighbourhood, Solution solution) => random_moves_without_replacement((TNeighbourhood)neighbourhood, (TSolution)solution).Cast<Move>();

  public Solution random_solution(Problem problem) => random_solution((TProblem)problem);

  public Solution revert_move(Move move, Solution solution) => revert_move((TMove)move, (TSolution)solution);
  #endregion
}

public record MutationMove<T, TS, TP>(int seed) : Move
  where TS : class, ISearchSpace<T> where TP : class, IProblem<T, TS>, Problem
{ }

public record MutationNeighborhood<T, TS, TP> : Neighbourhood
  where TS : class, ISearchSpace<T> where TP : class, IProblem<T, TS>
{ }

public class LazySolution<T> : Solution
{
  private bool evaluated;
  private bool bounded;
  private double? quality;
  private double? lowerBound;
  public readonly T candidate;
  private readonly IEvaluationContext<T> context;

  public LazySolution(T candidate, IEvaluationContext<T> context)
  {
    this.candidate = candidate;
    this.context = context;
  }

  private LazySolution(LazySolution<T> other)
  {
    candidate = other.candidate;
    context = other.context;
    evaluated = other.evaluated;
    bounded = other.bounded;
    quality = other.quality;
    lowerBound = other.lowerBound;
  }

  public double? Quality()
  {
    if (evaluated) return quality;
    quality = context.Evaluate(candidate, out var b, out var bound);
    if (b) {
      lowerBound = bound;
      bounded = true;
    }

    evaluated = true;
    return quality;
  }

  public double? LowerBound()
  {
    if (bounded) return lowerBound;
    lowerBound = context.LowerBound(candidate, out var e, out var q);
    if (e) {
      evaluated = true;
      quality = q;
    }

    return lowerBound;
  }

  public LazySolution<T> Copy() => new(this);
}

public interface IEvaluationContext<in T>
{
  public double? Evaluate(T input, out bool bounded, out double? bound);
  public double? LowerBound(T input, out bool evaluated, out double? quality);
}

public record ProblemOperations<T, TS, TP>(
  StatelessCreator<T, TS, TP> Creator,
  StatelessMutator<T, TS, TP> Mutator,
  TS SearchSpace,
  TP Problem,
  IRandomNumberGenerator rng)
  : BaseOperations<LazySolution<T>, MutationMove<T, TS, TP>, MutationNeighborhood<T, TS, TP>, TP>, IEvaluationContext<T>
  where TS : class, ISearchSpace<T> where TP : class, IProblem<T, TS>, Problem
{
  public override LazySolution<T> apply_move(MutationMove<T, TS, TP> move, LazySolution<T> solution)
    => new(Mutator.Mutate([solution.candidate], rng.Fork(move.seed), SearchSpace, Problem)[0], this);

  public override MutationNeighborhood<T, TS, TP> construction_neighbourhood(TP problem) => throw new NotSupportedException();

  public override LazySolution<T> copy_solution(LazySolution<T> solution) => solution.Copy();

  public override MutationNeighborhood<T, TS, TP> destruction_neighbourhood(TP problem) => throw new NotSupportedException();

  public override LazySolution<T> empty_solution(TP problem) => throw new NotSupportedException();

  public override LazySolution<T>? heuristic_solution(TP problem) => new(Creator.Create(1, rng, SearchSpace, Problem)[0], this);

  public override MutationNeighborhood<T, TS, TP> local_neighbourhood(TP problem) => new();

  public override double? lower_bound(LazySolution<T> solution) => solution.LowerBound();

  public override double? lower_bound_increment(MutationMove<T, TS, TP> move, LazySolution<T> solution) => apply_move(move, solution).LowerBound() - solution.LowerBound();

#pragma warning disable S2190
  public override IEnumerable<MutationMove<T, TS, TP>> moves(MutationNeighborhood<T, TS, TP> neighbourhood, LazySolution<T> solution)
#pragma warning restore S2190
  {
    while (true) {
      yield return new MutationMove<T, TS, TP>(rng.NextInt());
    }
  }

  public override double? objective_value(LazySolution<T> solution) => solution.LowerBound();

  public override double? objective_value_increment(MutationMove<T, TS, TP> move, LazySolution<T> solution) => apply_move(move, solution).Quality() - solution.Quality();

  public override MutationMove<T, TS, TP>? random_move(MutationNeighborhood<T, TS, TP> neighbourhood, LazySolution<T> solution) => new(rng.NextInt());

  public override IEnumerable<MutationMove<T, TS, TP>> random_moves_without_replacement(MutationNeighborhood<T, TS, TP> neighbourhood, LazySolution<T> solution) => throw new NotSupportedException();

  public override LazySolution<T> random_solution(TP problem) => new(Creator.Create(1, rng, SearchSpace, Problem)[0], this);

  public override LazySolution<T> revert_move(MutationMove<T, TS, TP> move, LazySolution<T> solution) => throw new NotSupportedException();

  public double? Evaluate(T input, out bool bounded, out double? bound)
  {
    if (!Problem.SearchSpace.Contains(input)) throw new NotImplementedException();
    bound = Problem.Evaluate([input], rng)[0][0];
    bounded = true;
    return bound;
  }

  public double? LowerBound(T input, out bool evaluated, out double? quality) => Evaluate(input, out evaluated, out quality);
}
