using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

public interface ITerminator<in TGenotype, in TIterationResult, in TSearchSpace, in TProblem>
  where TIterationResult : IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : IProblem<TGenotype, TSearchSpace>
{
  bool ShouldTerminate(TIterationResult currentIterationState, TIterationResult? previousIterationState, TSearchSpace encoding, TProblem problem);

  bool ShouldContinue(TIterationResult currentIterationState, TIterationResult? previousIterationState, TSearchSpace encoding, TProblem problem) => !ShouldTerminate(currentIterationState, previousIterationState, encoding, problem);
}

public interface ITerminator<TGenotype, in TIterationResult, in TSearchSpace> : ITerminator<TGenotype, TIterationResult, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  where TIterationResult : IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>;

public interface ITerminator<TGenotype, in TIterationResult> : ITerminator<TGenotype, TIterationResult, ISearchSpace<TGenotype>>
  where TIterationResult : IAlgorithmState;

public interface ITerminator<TGenotype> : ITerminator<TGenotype, IAlgorithmState>;
