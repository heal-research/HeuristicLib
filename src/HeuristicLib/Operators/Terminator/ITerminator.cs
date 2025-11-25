using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Operators.Terminator;

public interface ITerminator<in TGenotype, in TIterationResult, in TEncoding, in TProblem>
  where TIterationResult : IIterationResult
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding> {
  bool ShouldTerminate(TIterationResult currentIterationState, TIterationResult? previousIterationState, TEncoding encoding, TProblem problem);

  bool ShouldContinue(TIterationResult currentIterationState, TIterationResult? previousIterationState, TEncoding encoding, TProblem problem) {
    return !ShouldTerminate(currentIterationState, previousIterationState, encoding, problem);
  }
}

public interface ITerminator<TGenotype, in TIterationResult, in TEncoding> : ITerminator<TGenotype, TIterationResult, TEncoding, IProblem<TGenotype, TEncoding>>
  where TIterationResult : IIterationResult
  where TEncoding : class, IEncoding<TGenotype>;

public interface ITerminator<TGenotype, in TIterationResult> : ITerminator<TGenotype, TIterationResult, IEncoding<TGenotype>>
  where TIterationResult : IIterationResult;

public interface ITerminator<TGenotype> : ITerminator<TGenotype, IIterationResult>;
