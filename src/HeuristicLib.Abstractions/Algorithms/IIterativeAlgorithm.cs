using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms;

public interface IIterativeAlgorithm<TGenotype, in TSearchSpace, in TProblem, TAlgorithmState>
  : IAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TAlgorithmState : class, IAlgorithmState
{
  IInterceptor<TGenotype, TAlgorithmState, TSearchSpace, TProblem>? Interceptor { get; }
  
  TAlgorithmState ExecuteStep(TAlgorithmState? previousState, TProblem problem, IRandomNumberGenerator random);
}
