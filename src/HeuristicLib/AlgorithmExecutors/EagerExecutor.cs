using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.AlgorithmExecutors;

public class EagerExecutor : IAlgorithmExecutor {
  public TR Execute<TG, TS, TP, TR>(IAlgorithm<TG, TS, TP, TR> algorithm, TS searchSpace, TP problem, TR? initialState = default, IRandomNumberGenerator? random = null) where TG : class where TS : class, ISearchSpace<TG> where TP : class, IProblem<TG, TS> where TR : IAlgorithmState {
    return ExecuteAllSteps(algorithm, searchSpace, problem, initialState, random)[^1];
  }
  
  public IReadOnlyList<TR> ExecuteAllSteps<TG, TS, TP, TR>(IAlgorithm<TG, TS, TP, TR> algorithm, TS searchSpace, TP problem, TR? initialState = default, IRandomNumberGenerator? random = null) where TG : class where TS : class, ISearchSpace<TG> where TP : class, IProblem<TG, TS> where TR : IAlgorithmState {
    var states = new List<TR>();
    var streamingExecutor = new StreamingExecutor();
    foreach (var state in streamingExecutor.ExecuteStreaming(algorithm, searchSpace, problem, initialState, random)) {
      states.Add(state);
    }
    return states;
  }
}
