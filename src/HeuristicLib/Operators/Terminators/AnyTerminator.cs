using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

public class AnyTerminator<TGenotype, TIterationResult, TSearchSpace, TProblem>(params IReadOnlyList<ITerminator<TGenotype, TIterationResult, TSearchSpace, TProblem>> terminators)
  : Terminator<TGenotype, TIterationResult, TSearchSpace, TProblem>
  where TIterationResult : IIterationResult
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace> {
  public IReadOnlyList<ITerminator<TGenotype, TIterationResult, TSearchSpace, TProblem>> Terminators { get; } = terminators;

  public override bool ShouldTerminate(TIterationResult currentIterationState, TIterationResult? previousIterationState, TSearchSpace searchSpace, TProblem problem) {
    return Terminators.Any(criterion => criterion.ShouldTerminate(currentIterationState, previousIterationState, searchSpace, problem));
  }
}
