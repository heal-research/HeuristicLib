using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms;

public abstract class IterativeAlgorithm<TGenotype, TSearchSpace, TProblem, TIterationState>
  : Algorithm<TGenotype, TSearchSpace, TProblem, TIterationState>,
    IIterativeAlgorithm<TGenotype, TSearchSpace, TProblem, TIterationState>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TIterationState : class, IIterationState
{
  //public int CurrentIteration { get; protected set; }

  public required IRandomNumberGenerator AlgorithmRandom { get; init; }
  public required ITerminator<TGenotype, TIterationState, TSearchSpace, TProblem> Terminator { get; init; }
  public IInterceptor<TGenotype, TIterationState, TSearchSpace, TProblem>? Interceptor { get; init; }
  public required IEvaluator<TGenotype, TSearchSpace, TProblem> Evaluator { get; init; }
  public required ICreator<TGenotype, TSearchSpace, TProblem> Creator { get; init; }

  public abstract TIterationState ExecuteStep(TProblem problem, TSearchSpace searchSpace, TIterationState? previousState, IRandomNumberGenerator random);

  public override TIterationState Execute(TProblem problem, TSearchSpace? searchSpace = null, IRandomNumberGenerator? random = null) {
    return ExecuteStep(problem, problem.SearchSpace, previousState: null, random: random ?? AlgorithmRandom);
  }

  // public virtual TIterationState Execute(TProblem problem, TSearchSpace? searchSpace = null, TIterationState? previousState = default, IRandomNumberGenerator? random = null) {
  //   return ExecuteStreaming(problem, searchSpace, previousState, random).LastOrDefault() ?? throw new InvalidOperationException("The algorithm did not produce any iteration result.");
  // }

  // public IEnumerable<TIterationState> ExecuteStreaming(TProblem problem, TSearchSpace? searchSpace = null, TIterationState? previousState = default, IRandomNumberGenerator? random = null) {
  //   CheckSearchSpaceCompatible(problem, searchSpace);
  //   bool shouldContinue = previousState is null || Terminator.ShouldContinue(previousState, previousIterationState: default, searchSpace ?? problem.SearchSpace, problem);
  //
  //   while (shouldContinue) {
  //     var newIterationResult = ExecuteStep(problem, searchSpace ?? problem.SearchSpace, previousState, random ?? AlgorithmRandom);
  //     if (Interceptor != null) newIterationResult = Interceptor.Transform(newIterationResult, previousState, searchSpace ?? problem.SearchSpace, problem);
  //     CurrentIteration += 1;
  //     yield return newIterationResult;
  //     shouldContinue = Terminator.ShouldContinue(newIterationResult, previousState, searchSpace ?? problem.SearchSpace, problem);
  //     previousState = newIterationResult;
  //   }
  // }

  // private static void CheckSearchSpaceCompatible(TProblem problem, TSearchSpace? searchSpace) {
  //   if (searchSpace is ISubSearchSpaceComparable<TSearchSpace> s && !s.IsSubspaceOf(problem.SearchSpace))
  //     throw new ArgumentException("The provided search space is not a subspace of the problem's search space.");
  // }

  // public Population<TGenotype> CreateInitialPopulation(TProblem problem, TSearchSpace searchSpace, IRandomNumberGenerator random, int populationSize) {
  //   var population = Creator.Create(populationSize, random, searchSpace, problem);
  //   var objectives = Evaluator.Evaluate(population, random, searchSpace, problem);
  //   return Population.From(population, objectives);
  // }
}
