using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

public class AllTerminator<TGenotype, TAlgorithmState, TSearchSpace, TProblem>(params IReadOnlyList<ITerminator<TGenotype, TAlgorithmState, TSearchSpace, TProblem>> terminators)
  : Terminator<TGenotype, TAlgorithmState, TSearchSpace, TProblem>
  where TAlgorithmState : IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace> {
  public IReadOnlyList<ITerminator<TGenotype, TAlgorithmState, TSearchSpace, TProblem>> Terminators { get; } = terminators;

  public override bool ShouldTerminate(TAlgorithmState currentIterationState, TAlgorithmState? previousIterationState, TSearchSpace searchSpace, TProblem problem) {
    return Terminators.All(criterion => criterion.ShouldTerminate(currentIterationState, previousIterationState, searchSpace, problem));
  }
}
