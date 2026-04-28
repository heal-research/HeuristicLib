using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms;

public interface IIterativeAlgorithm<TGenotype, in TSearchSpace, in TProblem, TSearchState>
  : IAlgorithm<TGenotype, TSearchSpace, TProblem, TSearchState>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TSearchState : class, ISearchState
{
  IInterceptor<TGenotype, TSearchSpace, TProblem, TSearchState>? Interceptor { get; }
}
