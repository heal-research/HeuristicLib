using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Observation;

public interface IIterationObserver<in TG, in TS, in TP, in TR> 
  where TG : class
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TR : class, IAlgorithmState
{
  void OnIterationCompleted(TR currentState, TR? previousState, TS searchSpace, TP problem);
}
