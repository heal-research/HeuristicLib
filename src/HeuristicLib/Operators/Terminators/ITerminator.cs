using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

public interface ITerminator<TGenotype, in TAlgorithmState, in TSearchSpace> : ITerminator<TGenotype, TAlgorithmState, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  where TAlgorithmState : IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>;

public interface ITerminator<TGenotype, in TAlgorithmState> : ITerminator<TGenotype, TAlgorithmState, ISearchSpace<TGenotype>>
  where TAlgorithmState : IAlgorithmState;

public interface ITerminator<TGenotype> : ITerminator<TGenotype, IAlgorithmState>;
