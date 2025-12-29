using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

public interface ITerminator<TGenotype, in TIterationState, in TSearchSpace> : ITerminator<TGenotype, TIterationState, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  where TIterationState : IIterationState
  where TSearchSpace : class, ISearchSpace<TGenotype>;

public interface ITerminator<TGenotype, in TIterationState> : ITerminator<TGenotype, TIterationState, ISearchSpace<TGenotype>>
  where TIterationState : IIterationState;

public interface ITerminator<TGenotype> : ITerminator<TGenotype, IIterationState>;
