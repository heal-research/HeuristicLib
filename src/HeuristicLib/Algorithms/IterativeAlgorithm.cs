using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms;

public abstract class IterativeAlgorithm<TGenotype, TSearchSpace, TProblem, TIterationResult>
  : Algorithm<TGenotype, TSearchSpace, TProblem, TIterationResult>,
    IIterativeAlgorithm<TGenotype, TSearchSpace, TProblem, TIterationResult>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TIterationResult : class, IIterationResult {
  public int CurrentIteration { get; protected set; }

  public required IRandomNumberGenerator AlgorithmRandom { get; init; }
  public required ITerminator<TGenotype, TIterationResult, TSearchSpace, TProblem> Terminator { get; init; }
  public IInterceptor<TGenotype, TIterationResult, TSearchSpace, TProblem>? Interceptor { get; init; }
  public required IEvaluator<TGenotype, TSearchSpace, TProblem> Evaluator { get; init; }
  public required ICreator<TGenotype, TSearchSpace, TProblem> Creator { get; init; }

  public abstract TIterationResult ExecuteStep(TProblem problem, TSearchSpace searchSpace, TIterationResult? previousIterationResult, IRandomNumberGenerator random);

  public override TIterationResult Execute(TProblem problem, TSearchSpace? searchSpace = null, IRandomNumberGenerator? random = null) => Execute(problem, searchSpace, previousIterationResult: default, random);

  public virtual TIterationResult Execute(TProblem problem, TSearchSpace? searchSpace = null, TIterationResult? previousIterationResult = default, IRandomNumberGenerator? random = null) {
    return ExecuteStreaming(problem, searchSpace, previousIterationResult, random).LastOrDefault() ?? throw new InvalidOperationException("The algorithm did not produce any iteration result.");
  }

  public IEnumerable<TIterationResult> ExecuteStreaming(TProblem problem, TSearchSpace? searchSpace = null, TIterationResult? previousIterationResult = default, IRandomNumberGenerator? random = null) {
    CheckSearchSpaceCompatible(problem, searchSpace);
    bool shouldContinue = previousIterationResult is null || Terminator.ShouldContinue(previousIterationResult, previousIterationState: default, searchSpace ?? problem.SearchSpace, problem);

    while (shouldContinue) {
      var newIterationResult = ExecuteStep(problem, searchSpace ?? problem.SearchSpace, previousIterationResult, random ?? AlgorithmRandom);
      if (Interceptor != null) newIterationResult = Interceptor.Transform(newIterationResult, previousIterationResult, searchSpace ?? problem.SearchSpace, problem);
      CurrentIteration += 1;
      yield return newIterationResult;
      shouldContinue = Terminator.ShouldContinue(newIterationResult, previousIterationResult, searchSpace ?? problem.SearchSpace, problem);
      previousIterationResult = newIterationResult;
    }
  }

  private static void CheckSearchSpaceCompatible(TProblem problem, TSearchSpace? searchSpace) {
    if (searchSpace is ISubSearchSpaceComparable<TSearchSpace> s && !s.IsSubspaceOf(problem.SearchSpace))
      throw new ArgumentException("The provided search space is not a subspace of the problem's search space.");
  }

  public Population<TGenotype> CreateInitialPopulation(TProblem problem, TSearchSpace searchSpace, IRandomNumberGenerator random, int populationSize) {
    var population = Creator.Create(populationSize, random, searchSpace, problem);
    var objectives = Evaluator.Evaluate(population, random, searchSpace, problem);
    return Population.From(population, objectives);
  }
}
