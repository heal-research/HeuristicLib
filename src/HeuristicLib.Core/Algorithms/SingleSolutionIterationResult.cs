using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Algorithms;

public record SingleISolutionAlgorithmState<T>(ISolution<T> Solution) : PopulationAlgorithmState<T>(new Population<T>(Solution))
{
  public ISolution<T> Solution => Population.Solutions[0];
}
