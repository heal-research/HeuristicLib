using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Analysis;

public abstract record AlgorithmPerformanceEvaluator<T, TS, TP, TSearchState, TExecutionState>(IAlgorithm<T, TS, TP, TSearchState> Algorithm) : Analyzer<T, TS, TP, TSearchState, TExecutionState>(Algorithm)
  where TS : class, ISearchSpace<T>
  where TP : class, IProblem<T, TS>
  where TSearchState : class, ISearchState
  where TExecutionState : class, IAlgorithmPerformanceState
{
  public abstract Objective Objective { get; }
}
