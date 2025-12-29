using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

public interface ITerminator<TGenotype, in TIterationResult, in TSearchSpace> : ITerminator<TGenotype, TIterationResult, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  where TIterationResult : IIterationResult
  where TSearchSpace : class, ISearchSpace<TGenotype>;

public interface ITerminator<TGenotype, in TIterationResult> : ITerminator<TGenotype, TIterationResult, ISearchSpace<TGenotype>>
  where TIterationResult : IIterationResult;

public interface ITerminator<TGenotype> : ITerminator<TGenotype, IIterationResult>;
