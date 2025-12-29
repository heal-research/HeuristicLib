using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.AlgorithmExecutors;

public class StreamingExecutor : IAlgorithmExecutor {
  public TR Execute<TG, TS, TP, TR>(IAlgorithm<TG, TS, TP, TR> algorithm, TS searchSpace, TP problem, TR? initialState = default, IRandomNumberGenerator? random = null) where TG : class where TS : class, ISearchSpace<TG> where TP : class, IProblem<TG, TS> where TR : IAlgorithmState {
    return ExecuteStreaming(algorithm, searchSpace, problem, initialState, random).Last();
  }

  public IEnumerable<TR> ExecuteStreaming<TG, TS, TP, TR>(IAlgorithm<TG, TS, TP, TR> algorithm, TS searchSpace, TP problem, TR? initialState = default, IRandomNumberGenerator? random = null) where TG : class where TS : class, ISearchSpace<TG> where TP : class, IProblem<TG, TS> where TR : IAlgorithmState {
    CheckSearchSpaceCompatible<TG, TS, TP>(problem, searchSpace);
    
    TR? previousState = initialState;
    bool shouldContinue = previousState is null || algorithm.Terminator.ShouldContinue(previousState, previousIterationState: default, searchSpace ?? problem.SearchSpace, problem);

    while (shouldContinue) {
      var newIterationState = algorithm.ExecuteStep(problem, searchSpace ?? problem.SearchSpace, previousState, random ?? algorithm.AlgorithmRandom);
      if (algorithm.Interceptor != null) {
        newIterationState = algorithm.Interceptor.Transform(newIterationState, previousState, searchSpace ?? problem.SearchSpace, problem);
      }
      
      yield return newIterationState;
        
      shouldContinue = algorithm.Terminator.ShouldContinue(newIterationState, previousState, searchSpace ?? problem.SearchSpace, problem);
      previousState = newIterationState;
    }
  }
  
  [Obsolete("Move to Search Space (extensions?)")]
  private void CheckSearchSpaceCompatible<TG, TS, TP>(TP problem, TS? searchSpace) 
    where TG : class
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS> 
  {
    if (searchSpace is ISubSearchSpaceComparable<TS> s && !s.IsSubspaceOf(problem.SearchSpace))
      throw new ArgumentException("The provided search space is not a subspace of the problem's search space.");
  }
}
