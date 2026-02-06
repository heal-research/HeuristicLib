using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators;

public interface ITerminator<in TGenotype, in TAlgorithmState, in TSearchSpace, in TProblem>
  where TAlgorithmState : IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : IProblem<TGenotype, TSearchSpace>
{
  Func<TAlgorithmState, bool> CreateShouldTerminatePredicate(TSearchSpace searchSpace, TProblem problem);
  Func<TAlgorithmState, bool> CreateShouldContinuePredicate(TSearchSpace searchSpace, TProblem problem)
  {
    var predicate = CreateShouldTerminatePredicate(searchSpace, problem);
    return state => !predicate(state);
  }
}
