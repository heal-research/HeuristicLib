using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

public class AnyTerminator<TGenotype, TIterationState, TSearchSpace, TProblem>(params IReadOnlyList<ITerminator<TGenotype, TIterationState, TSearchSpace, TProblem>> terminators)
  : Terminator<TGenotype, TIterationState, TSearchSpace, TProblem>
  where TIterationState : IIterationState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace> {
  public IReadOnlyList<ITerminator<TGenotype, TIterationState, TSearchSpace, TProblem>> Terminators { get; } = terminators;

  public override bool ShouldTerminate(TIterationState currentIterationState, TIterationState? previousIterationState, TSearchSpace searchSpace, TProblem problem) {
    return Terminators.Any(criterion => criterion.ShouldTerminate(currentIterationState, previousIterationState, searchSpace, problem));
  }
}
