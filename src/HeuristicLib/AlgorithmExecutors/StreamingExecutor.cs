using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.AlgorithmExecutors;

public class StreamingExecutor : IAlgorithmExecutor {
  public TR Execute<TG, TS, TP, TR>(IAlgorithm<TG, TS, TP, TR> algorithm, TP problem,  IRandomNumberGenerator random, TR? initialState = default) 
    where TG : class where TS : class, ISearchSpace<TG> where TP : class, IProblem<TG, TS> where TR : IAlgorithmState {
    return ExecuteStreaming(algorithm, problem, random, initialState).Last();
  }

  public IEnumerable<TR> ExecuteStreaming<TG, TS, TP, TR>(IAlgorithm<TG, TS, TP, TR> algorithm, TP problem, IRandomNumberGenerator random, TR? initialState = default) 
    where TG : class where TS : class, ISearchSpace<TG> where TP : class, IProblem<TG, TS> where TR : IAlgorithmState 
  {
    
    TR? previousState = initialState;
    bool shouldContinue = previousState is null || algorithm.Terminator.ShouldContinue(previousState, previousIterationState: default, problem.SearchSpace, problem);

    while (shouldContinue) {
      var newIterationState = algorithm.ExecuteStep(problem, previousState, random);
      if (algorithm.Interceptor is not null) {
        newIterationState = algorithm.Interceptor.Transform(newIterationState, previousState, problem.SearchSpace, problem);
      }
      
      yield return newIterationState;
        
      shouldContinue = algorithm.Terminator.ShouldContinue(newIterationState, previousState, problem.SearchSpace, problem);
      previousState = newIterationState;
    }
  }
}
