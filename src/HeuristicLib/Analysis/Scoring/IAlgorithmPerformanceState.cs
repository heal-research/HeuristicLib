using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Analysis.Scoring;

public interface IAlgorithmPerformanceState
{
    ObjectiveVector CurrentScore { get; }
}
