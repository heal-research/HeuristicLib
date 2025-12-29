using HEAL.HeuristicLib.AlgorithmExecutors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms;

public static class AlgorithmExtensions {
  extension<TG, TS, TP, TR>(IAlgorithm<TG, TS, TP, TR> algorithm)
    where TG : class
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
    where TR : IAlgorithmState
    {
      public TR Execute(TP problem, TS? searchSpace = null, TR? previousState = default, IRandomNumberGenerator? random = null) {
        return new StreamingExecutor().Execute(algorithm, searchSpace ?? problem.SearchSpace, problem, previousState, random);
      }
      
      public IEnumerable<TR> ExecuteStreaming(TP problem, TS? searchSpace = null, TR? previousState = default, IRandomNumberGenerator? random = null) {
        return new StreamingExecutor().ExecuteStreaming(algorithm, searchSpace ?? problem.SearchSpace, problem, previousState, random);
      }
    }
}
