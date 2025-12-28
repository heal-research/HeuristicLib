using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminator;

public class AllTerminator<TGenotype, TIterationResult, TEncoding, TProblem>(params IReadOnlyList<ITerminator<TGenotype, TIterationResult, TEncoding, TProblem>> terminators)
  : Terminator<TGenotype, TIterationResult, TEncoding, TProblem>
  where TIterationResult : IIterationResult
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  public IReadOnlyList<ITerminator<TGenotype, TIterationResult, TEncoding, TProblem>> Terminators { get; } = terminators;

  public override bool ShouldTerminate(TIterationResult currentIterationState, TIterationResult? previousIterationState, TEncoding encoding, TProblem problem) {
    return Terminators.All(criterion => criterion.ShouldTerminate(currentIterationState, previousIterationState, encoding, problem));
  }
}
