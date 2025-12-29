using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms;

public interface IAlgorithmBuilder<TG, TS, TP, TR, out TAlgorithm>
  where TAlgorithm : IAlgorithm<TG, TS, TP, TR>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TR : IAlgorithmState
  where TG : class
{
  
  TAlgorithm Build();
}
