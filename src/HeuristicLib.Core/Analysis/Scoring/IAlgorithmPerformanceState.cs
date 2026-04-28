using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Analysis;

public interface IAlgorithmPerformanceState
{
  ObjectiveVector CurrentScore { get; }
}
