using HEAL.HeuristicLib.Operators.Interceptor;
using HEAL.HeuristicLib.Operators.Terminator;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Algorithms;

public abstract class IterativeAlgorithm<TGenotype, TEncoding, TProblem, TIterationResult>(ITerminator<TGenotype, TIterationResult, TEncoding, TProblem> terminator, int? randomSeed, IInterceptor<TGenotype, TIterationResult, TEncoding, TProblem>? interceptor)
  : Algorithm<TGenotype, TEncoding, TProblem, TIterationResult>,
    IIterativeAlgorithm<TGenotype, TEncoding, TProblem, TIterationResult>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding>
  where TIterationResult : IIterationResult {
  public int CurrentIteration { get; protected set; }

  protected readonly IRandomNumberGenerator AlgorithmRandom = SystemRandomNumberGenerator.Default(randomSeed);

  public ITerminator<TGenotype, TIterationResult, TEncoding, TProblem> Terminator { get; } = terminator;
  public IInterceptor<TGenotype, TIterationResult, TEncoding, TProblem>? Interceptor { get; } = interceptor;

  public abstract TIterationResult ExecuteStep(TProblem problem, TEncoding searchSpace, TIterationResult? previousIterationResult, IRandomNumberGenerator random);

  public override TIterationResult Execute(TProblem problem, TEncoding? searchSpace = null, IRandomNumberGenerator? random = null) {
    return Execute(problem, searchSpace, previousIterationResult: default, random);
  }

  public virtual TIterationResult Execute(TProblem problem, TEncoding? searchSpace = null, TIterationResult? previousIterationResult = default, IRandomNumberGenerator? random = null) {
    TIterationResult? lastResult = ExecuteStreaming(problem, searchSpace, previousIterationResult, random).LastOrDefault();

    if (lastResult is null) {
      throw new InvalidOperationException("The algorithm did not produce any iteration result.");
    }

    return lastResult;
  }

  public IEnumerable<TIterationResult> ExecuteStreaming(TProblem problem, TEncoding? searchSpace = null, TIterationResult? previousIterationResult = default, IRandomNumberGenerator? random = null) {
    CheckSearchSpaceCompatible(problem, searchSpace);
    bool shouldContinue = previousIterationResult is null ||
                          Terminator.ShouldContinue(previousIterationResult, previousIterationState: default, searchSpace ?? problem.SearchSpace, problem);

    while (shouldContinue) {
      var newIterationResult = ExecuteStep(problem, searchSpace ?? problem.SearchSpace, previousIterationResult, random ?? AlgorithmRandom);
      if (Interceptor != null) newIterationResult = Interceptor.Transform(newIterationResult, previousIterationResult, searchSpace ?? problem.SearchSpace, problem);
      CurrentIteration += 1;
      yield return newIterationResult;
      shouldContinue = Terminator.ShouldContinue(newIterationResult, previousIterationResult, searchSpace ?? problem.SearchSpace, problem);
      previousIterationResult = newIterationResult;
    }
  }

  private static void CheckSearchSpaceCompatible(TProblem problem, TEncoding? searchSpace) {
    if (searchSpace is ISubencodingComparable<TEncoding> s && !s.IsSubspaceOf(problem.SearchSpace))
      throw new ArgumentException("The provided search space is not a subspace of the problem's search space.");
  }
}
