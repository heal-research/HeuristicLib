using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

public interface ITerminator<in TGenotype, in TAlgorithmState, in TSearchSpace, in TProblem>
  where TAlgorithmState : IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : IProblem<TGenotype, TSearchSpace>
{
  bool ShouldTerminate(TAlgorithmState currentIterationState, TAlgorithmState? previousIterationState, TSearchSpace searchSpace, TProblem problem);

  bool ShouldContinue(TAlgorithmState currentIterationState, TAlgorithmState? previousIterationState, TSearchSpace searchSpace, TProblem problem) => !ShouldTerminate(currentIterationState, previousIterationState, searchSpace, problem);
}
