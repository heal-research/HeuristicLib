using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Analysis;

public class QualityScorerState : IAlgorithmPerformanceState
{
  public required ObjectiveVector CurrentScore { get; set; }
}
