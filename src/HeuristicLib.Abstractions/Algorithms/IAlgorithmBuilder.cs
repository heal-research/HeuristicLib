using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms;

public interface IAlgorithmBuilder
{
}

public interface IAlgorithmBuilder<TG, TS, TP, TR, out TA> : IAlgorithmBuilder
  where TA : IAlgorithm<TG, TS, TP, TR>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TR : class, IAlgorithmState
  where TG : class
{
  TA Build();
}
