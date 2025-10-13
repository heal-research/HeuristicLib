using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Algorithms.LocalSearch;

public class LocalSearch<TGenotype, TEncoding, TProblem>(
  ITerminator<TGenotype, SingleSolutionIterationResult<TGenotype>, TEncoding, TProblem> terminator,
  IInterceptor<TGenotype, SingleSolutionIterationResult<TGenotype>, TEncoding, TProblem>? interceptor,
  ICreator<TGenotype, TEncoding, TProblem> creator,
  IMutator<TGenotype, TEncoding, TProblem> mutator,
  int? randomSeed)
  : IterativeAlgorithm<TGenotype, TEncoding, TProblem, SingleSolutionSearchResult<TGenotype>, SingleSolutionIterationResult<TGenotype>>(terminator, interceptor)
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  public enum SearchDirection { FirstImprovement, BestImprovement }

  public ICreator<TGenotype, TEncoding, TProblem> Creator { get; } = creator;
  public IMutator<TGenotype, TEncoding, TProblem> Mutator { get; } = mutator;

  public SearchDirection Direction { get; } = SearchDirection.FirstImprovement;

  public int MaxNeighbors { get; } = int.MaxValue;
  public int BatchSize { get; set; } = 16;
  private readonly IRandomNumberGenerator algorithmRandom = new SystemRandomNumberGenerator(randomSeed ?? SystemRandomNumberGenerator.RandomSeed());

  public override SingleSolutionIterationResult<TGenotype> ExecuteStep(
    TProblem problem,
    TEncoding? searchSpace = default,
    SingleSolutionIterationResult<TGenotype>? previousIterationResult = default,
    IRandomNumberGenerator? random = default) {
    random ??= algorithmRandom;
    searchSpace ??= problem.SearchSpace;

    if (previousIterationResult == null) {
      var ind = Creator.Create(1, random, searchSpace, problem);
      var obj = problem.Evaluate(ind);
      return new SingleSolutionIterationResult<TGenotype>(new Solution<TGenotype>(ind[0], obj[0]));
    }

    var sol = previousIterationResult.Solution;
    Solution<TGenotype> newSolution = sol;

    for (int i = 0; i < MaxNeighbors; i++) {
      var child = Mutator.Mutate(Enumerable.Repeat(sol.Genotype, BatchSize).ToArray(), random, searchSpace, problem);
      var res = problem.Evaluate(child);
      var best = BestSelector.Select(res.Append(sol.ObjectiveVector).ToArray(), problem.Objective, 1, random)[0];
      if (best == BatchSize)
        continue;
      newSolution = new Solution<TGenotype>(child[best], res[best]);
      if (Direction == SearchDirection.FirstImprovement)
        return new SingleSolutionIterationResult<TGenotype>(newSolution);
    }

    return new SingleSolutionIterationResult<TGenotype>(newSolution);
  }

  protected override SingleSolutionSearchResult<TGenotype> FinalizeResult(SingleSolutionIterationResult<TGenotype> iterationResult, TProblem problem) =>
    new(iterationResult.Solution);
}

public record SingleSolutionIterationResult<T>(Solution<T> Solution) : IIterationResult<T>;

public record SingleSolutionSearchResult<T>(Solution<T> Solution) : IAlgorithmResult<T>;
