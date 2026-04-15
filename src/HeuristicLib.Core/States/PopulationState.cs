using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.States;

public record PopulationState<TGenotype> : IAlgorithmState
{
  public required Population<TGenotype> Population { get; init; }

  public static implicit operator PopulationState<TGenotype>(Population<TGenotype> population) => new() { Population = population };
  public static implicit operator Population<TGenotype>(PopulationState<TGenotype> state) => state.Population;
}
