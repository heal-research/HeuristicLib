using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators;

public interface ITerminator<TGenotype, in TSearchSpace, in TProblem, in TSearchState>
  : IOperator<ITerminatorInstance<TGenotype, TSearchSpace, TProblem, TSearchState>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : IProblem<TGenotype, TSearchSpace>
  where TSearchState : ISearchState;

public interface ITerminatorInstance<TGenotype, in TSearchSpace, in TProblem, in TSearchState>
  : IOperatorInstance
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : IProblem<TGenotype, TSearchSpace>
  where TSearchState : ISearchState
{
  bool ShouldTerminate(TSearchState state, TSearchSpace searchSpace, TProblem problem);
}
