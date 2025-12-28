using HEAL.HeuristicLib.States;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Terminator;

public interface ITerminator<in TGenotype, in TIterationResult, in TSearchSpace, in TProblem>
  where TIterationResult : IIterationResult
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : IProblem<TGenotype, TSearchSpace> {
  bool ShouldTerminate(TIterationResult currentIterationState, TIterationResult? previousIterationState, TSearchSpace searchSpace, TProblem problem);

  bool ShouldContinue(TIterationResult currentIterationState, TIterationResult? previousIterationState, TSearchSpace searchSpace, TProblem problem) {
    return !ShouldTerminate(currentIterationState, previousIterationState, searchSpace, problem);
  }
}
