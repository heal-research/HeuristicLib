using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Analysis.Scoring;

public abstract record AlgorithmPerformanceEvaluator<TExecutionState>() : Analyzer<TExecutionState>
    where TExecutionState : class, IAlgorithmPerformanceState
{
    public abstract ObjectiveDirections Objective { get; }
}
