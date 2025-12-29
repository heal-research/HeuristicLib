using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms;

public interface IIterativeAlgorithm<TGenotype, in TSearchSpace, in TProblem, TIterationState>
  : IAlgorithm<TGenotype, TSearchSpace, TProblem, TIterationState>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TIterationState : IIterationState 
{
  ITerminator<TGenotype, TIterationState, TSearchSpace, TProblem> Terminator { get; }
  IInterceptor<TGenotype, TIterationState, TSearchSpace, TProblem>? Interceptor { get; }
  
  TIterationState ExecuteStep(TProblem problem, TSearchSpace searchSpace, TIterationState? previousState, IRandomNumberGenerator random);
  //IEnumerable<TIterationState> ExecuteStreaming(TProblem problem, TSearchSpace? searchSpace = null, TIterationState? previousIterationResult = default, IRandomNumberGenerator? random = null);
}
