using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Algorithms;

public class PopulationResult<TGenotype>(Population<TGenotype> population) : IAlgorithmResult<TGenotype> {
  public Population<TGenotype> Population { get; } = population;
}
