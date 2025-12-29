using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms;

public interface IAlgorithm<TGenotype, in TSearchSpace, in TProblem, TAlgorithmState> 
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TAlgorithmState : IAlgorithmState
  where TGenotype : class
{
  // ToDo: think about removing the properties form the core interface
  IRandomNumberGenerator AlgorithmRandom { get; }
  
  ITerminator<TGenotype, TAlgorithmState, TSearchSpace, TProblem> Terminator { get; }
  IInterceptor<TGenotype, TAlgorithmState, TSearchSpace, TProblem>? Interceptor { get; }
  
  TAlgorithmState ExecuteStep(TProblem problem, TSearchSpace searchSpace, TAlgorithmState? previousState, IRandomNumberGenerator random);
}
