using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

public interface ITerminator<in TGenotype, in TIterationState, in TSearchSpace, in TProblem>
  where TIterationState : IIterationState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : IProblem<TGenotype, TSearchSpace> {
  bool ShouldTerminate(TIterationState currentIterationState, TIterationState? previousIterationState, TSearchSpace searchSpace, TProblem problem);

  bool ShouldContinue(TIterationState currentIterationState, TIterationState? previousIterationState, TSearchSpace searchSpace, TProblem problem) {
    return !ShouldTerminate(currentIterationState, previousIterationState, searchSpace, problem);
  }
}
