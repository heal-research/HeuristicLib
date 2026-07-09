using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Analysis.Scoring;

public abstract record AlgorithmPerformanceEvaluator<TCandidate, TSearchSpace, TProblem, TSearchState, TExecutionState>(IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> Algorithm) : Analyzer<TCandidate, TSearchSpace, TProblem, TSearchState, TExecutionState>(Algorithm)
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
  where TSearchState : class, ISearchState
  where TExecutionState : class, IAlgorithmPerformanceState
{
    public abstract ObjectiveDirections Objective { get; }
}
