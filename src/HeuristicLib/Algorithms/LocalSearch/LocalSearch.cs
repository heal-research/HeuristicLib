using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Analyzer;
using HEAL.HeuristicLib.Operators.Creator;
using HEAL.HeuristicLib.Operators.Evaluator;
using HEAL.HeuristicLib.Operators.Interceptor;
using HEAL.HeuristicLib.Operators.Mutator;
using HEAL.HeuristicLib.Operators.Selector;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Algorithms.LocalSearch;

public enum LocalSearchDirection { FirstImprovement, BestImprovement }

public class LocalSearch<TGenotype, TEncoding, TProblem>(
  ITerminator<TGenotype, SingleSolutionIterationResult<TGenotype>, TEncoding, TProblem> terminator,
  IInterceptor<TGenotype, SingleSolutionIterationResult<TGenotype>, TEncoding, TProblem>? interceptor,
  ICreator<TGenotype, TEncoding, TProblem> creator,
  IMutator<TGenotype, TEncoding, TProblem> mutator,
  IEvaluator<TGenotype, TEncoding, TProblem> evaluator,
  int? randomSeed,
  int maxNeighbors,
  int batchSize,
  LocalSearchDirection direction)
  : IterativeAlgorithm<TGenotype, TEncoding, TProblem, SingleSolutionResult<TGenotype>, SingleSolutionIterationResult<TGenotype>>(terminator, randomSeed, interceptor)
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  public ICreator<TGenotype, TEncoding, TProblem> Creator { get; } = creator;
  public IMutator<TGenotype, TEncoding, TProblem> Mutator { get; } = mutator;
  public IEvaluator<TGenotype, TEncoding, TProblem> Evaluator { get; } = evaluator;
  public LocalSearchDirection Direction { get; } = direction;
  public int MaxNeighbors { get; } = maxNeighbors;
  public int BatchSize { get; } = batchSize;

  public override SingleSolutionIterationResult<TGenotype> ExecuteStep(
    TProblem problem,
    TEncoding searchSpace,
    SingleSolutionIterationResult<TGenotype>? previousIterationResult,
    IRandomNumberGenerator random) {
    if (previousIterationResult == null) {
      var ind = Creator.Create(1, random, searchSpace, problem);
      var obj = Evaluator.Evaluate(ind, random, searchSpace, problem);
      return new SingleSolutionIterationResult<TGenotype>(new Solution<TGenotype>(ind[0], obj[0]));
    }

    var sol = previousIterationResult.Solution;
    Solution<TGenotype> newSolution = sol;

    for (int i = 0; i < MaxNeighbors; i += BatchSize) {
      var child = Mutator.Mutate(Enumerable.Repeat(sol.Genotype, BatchSize).ToArray(), random, searchSpace, problem);
      var res = Evaluator.Evaluate(child, random, searchSpace, problem);
      var best = BestSelector.Select(res.Append(sol.ObjectiveVector).ToArray(), problem.Objective, 1, random)[0];
      if (best == BatchSize)
        continue;
      newSolution = new Solution<TGenotype>(child[best], res[best]);
      if (Direction == LocalSearchDirection.FirstImprovement)
        return new SingleSolutionIterationResult<TGenotype>(newSolution);
    }

    return new SingleSolutionIterationResult<TGenotype>(newSolution);
  }

  protected override SingleSolutionResult<TGenotype> FinalizeResult(SingleSolutionIterationResult<TGenotype> iterationResult, TProblem problem) =>
    new(iterationResult.Solution);
}

public static class LocalSearch {
  public static LocalSearch<TGenotype, TEncoding, TProblem> Create<TGenotype, TEncoding, TProblem>(
    ICreator<TGenotype, TEncoding, TProblem> creator,
    IMutator<TGenotype, TEncoding, TProblem> mutator,
    ITerminator<TGenotype, SingleSolutionIterationResult<TGenotype>, TEncoding, TProblem> terminator,
    IEvaluator<TGenotype, TEncoding, TProblem> evaluator,
    int? randomSeed,
    int maxNeighbors,
    int batchSize,
    LocalSearchDirection direction,
    IInterceptor<TGenotype, SingleSolutionIterationResult<TGenotype>, TEncoding, TProblem>? interceptor = null,
    params IAnalyzer<TGenotype, SingleSolutionIterationResult<TGenotype>, TEncoding, TProblem>[] analyzers)
    where TEncoding : class, IEncoding<TGenotype>
    where TProblem : class, IProblem<TGenotype, TEncoding>
    => new(terminator, MultiInterceptor.Create(interceptor, analyzers), creator, mutator, evaluator, randomSeed, maxNeighbors, batchSize, direction);
}
