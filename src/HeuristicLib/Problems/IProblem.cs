using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems;

public interface IProblem {
  Objective Objective { get; }
}
