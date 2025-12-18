using HEAL.HeuristicLib.Operators.Creator;
using HEAL.HeuristicLib.Operators.Evaluator;
using HEAL.HeuristicLib.Operators.Interceptor;
using HEAL.HeuristicLib.Operators.Terminator;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Algorithms;

public abstract class IterativeAlgorithm<TGenotype, TEncoding, TProblem, TIterationResult>
  : Algorithm<TGenotype, TEncoding, TProblem, TIterationResult>,
    IIterativeAlgorithm<TGenotype, TEncoding, TProblem, TIterationResult>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding>
  where TIterationResult : class, IIterationResult {
  public int CurrentIteration { get; protected set; }

  public required IRandomNumberGenerator AlgorithmRandom { get; init; }
  public required ITerminator<TGenotype, TIterationResult, TEncoding, TProblem> Terminator { get; init; }
  public IInterceptor<TGenotype, TIterationResult, TEncoding, TProblem>? Interceptor { get; init; }
  public required IEvaluator<TGenotype, TEncoding, TProblem> Evaluator { get; init; }
  public required ICreator<TGenotype, TEncoding, TProblem> Creator { get; init; }

  public abstract TIterationResult ExecuteStep(TProblem problem, TEncoding searchSpace, TIterationResult? previousIterationResult, IRandomNumberGenerator random);

  public override TIterationResult Execute(TProblem problem, TEncoding? searchSpace = null, IRandomNumberGenerator? random = null) => Execute(problem, searchSpace, previousIterationResult: null, random);

  public virtual TIterationResult Execute(TProblem problem, TEncoding? searchSpace = null, TIterationResult? previousIterationResult = null, IRandomNumberGenerator? random = null) {
    return ExecuteStreaming(problem, searchSpace, previousIterationResult, random).LastOrDefault() ?? throw new InvalidOperationException("The algorithm did not produce any iteration result.");
  }

  public IEnumerable<TIterationResult> ExecuteStreaming(TProblem problem, TEncoding? searchSpace = null, TIterationResult? previousIterationResult = null, IRandomNumberGenerator? random = null) {
    CheckSearchSpaceCompatible(problem, searchSpace);
    var problemSearchSpace = searchSpace ?? problem.SearchSpace;
    var shouldContinue = previousIterationResult is null || Terminator.ShouldContinue(previousIterationResult, previousIterationState: null, problemSearchSpace, problem);
    var randomNumberGenerator = random ?? AlgorithmRandom;
    while (shouldContinue) {
      var newIterationResult = ExecuteStep(problem, problemSearchSpace, previousIterationResult, randomNumberGenerator);
      if (Interceptor != null) newIterationResult = Interceptor.Transform(newIterationResult, previousIterationResult, randomNumberGenerator, problemSearchSpace, problem);
      CurrentIteration += 1;
      yield return newIterationResult;
      shouldContinue = Terminator.ShouldContinue(newIterationResult, previousIterationResult, problemSearchSpace, problem);
      previousIterationResult = newIterationResult;
    }
  }

  private static void CheckSearchSpaceCompatible(TProblem problem, TEncoding? searchSpace) {
    if (searchSpace is ISubencodingComparable<TEncoding> s && !s.IsSubspaceOf(problem.SearchSpace))
      throw new ArgumentException("The provided search space is not a subspace of the problem's search space.");
  }

  public Population<TGenotype> CreateInitialPopulation(TProblem problem, TEncoding searchSpace, IRandomNumberGenerator random, int populationSize) {
    var population = Creator.Create(populationSize, random, searchSpace, problem);
    var objectives = Evaluator.Evaluate(population, random, searchSpace, problem);
    return Population.From(population, objectives);
  }
}
