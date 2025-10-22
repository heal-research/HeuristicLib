using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Algorithms;

public class SingleSolutionIterationResult<T>(Solution<T> solution) : PopulationIterationResult<T>(new Population<T>([solution])) {
  public Solution<T> Solution => Population.Solutions[0];
}
