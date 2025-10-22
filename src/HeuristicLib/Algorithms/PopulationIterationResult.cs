using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Algorithms;

public class PopulationIterationResult<TGenotype>(Population<TGenotype> population) : IIterationResult {
  public Population<TGenotype> Population { get; } = population;
}
