using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Analysis.Scoring;

public class QualityScorerState : IAlgorithmPerformanceState
{
  public required ObjectiveVector CurrentScore { get; set; }
}
