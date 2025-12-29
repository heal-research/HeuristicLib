using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.AlgorithmExecutors;

public class EagerExecutor : IAlgorithmExecutor {
  public TR Execute<TG, TS, TP, TR>(IAlgorithm<TG, TS, TP, TR> algorithm, TP problem,  IRandomNumberGenerator random, TR? initialState = default)
    where TG : class where TS : class, ISearchSpace<TG> where TP : class, IProblem<TG, TS> where TR : IAlgorithmState {
    return ExecuteAllSteps(algorithm, problem, random, initialState)[^1];
  }
  
  public IReadOnlyList<TR> ExecuteAllSteps<TG, TS, TP, TR>(IAlgorithm<TG, TS, TP, TR> algorithm, TP problem, IRandomNumberGenerator random, TR? initialState = default) 
    where TG : class where TS : class, ISearchSpace<TG> where TP : class, IProblem<TG, TS> where TR : IAlgorithmState {
    var states = new List<TR>();
    var streamingExecutor = new StreamingExecutor();
    foreach (var state in streamingExecutor.ExecuteStreaming(algorithm, problem, random, initialState)) {
      states.Add(state);
    }
    return states;
  }
}
