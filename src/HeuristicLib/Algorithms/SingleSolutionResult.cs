using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Algorithms;

public class SingleSolutionResult<T>(Solution<T> solution) : PopulationResult<T>(new Population<T>([solution])) {
  public Solution<T> Solution => Population.Solutions[0];
}
