using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms;

public interface IAlgorithmBuilder;

#pragma warning disable S2326
public interface IAlgorithmBuilder<TG, TS, TP, TR>
#pragma warning restore S2326
  : IAlgorithmBuilder
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TR : class, IAlgorithmState;

public interface IAlgorithmBuilder<TG, TS, TP, TR, out TA>
  : IAlgorithmBuilder<TG, TS, TP, TR>
  where TA : IAlgorithm<TG, TS, TP, TR>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TR : class, IAlgorithmState
{
  TA Build();
}
