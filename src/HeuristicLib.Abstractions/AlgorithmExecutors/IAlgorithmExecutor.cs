using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.AlgorithmExecutors;

public interface IAlgorithmExecutor
{
  // ToDo: Maybe more parameters?
  TR Execute<TG, TS, TP, TR>(IAlgorithm<TG, TS, TP, TR> algorithm, TS searchSpace, TP problem, TR? initialState = default, IRandomNumberGenerator? random = null)
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
    where TR : IAlgorithmState
    where TG : class;
}
