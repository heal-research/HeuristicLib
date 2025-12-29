using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

public interface ITerminator<in TGenotype, in TIterationResult, in TSearchSpace, in TProblem>
  where TIterationResult : IIterationResult
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : IProblem<TGenotype, TSearchSpace> {
  bool ShouldTerminate(TIterationResult currentIterationState, TIterationResult? previousIterationState, TSearchSpace searchSpace, TProblem problem);

  bool ShouldContinue(TIterationResult currentIterationState, TIterationResult? previousIterationState, TSearchSpace searchSpace, TProblem problem) {
    return !ShouldTerminate(currentIterationState, previousIterationState, searchSpace, problem);
  }
}
