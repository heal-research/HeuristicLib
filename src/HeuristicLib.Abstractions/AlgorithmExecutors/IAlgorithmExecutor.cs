using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.AlgorithmExecutors;

public interface IAlgorithmExecutor
{
  TR Execute<TG, TS, TP, TR>(IAlgorithm<TG, TS, TP, TR> algorithm, TP problem, IRandomNumberGenerator random, TR? initialState = default)
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
    where TR : IAlgorithmState
    where TG : class;
}
