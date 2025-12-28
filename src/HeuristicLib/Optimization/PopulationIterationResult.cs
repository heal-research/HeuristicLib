using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Optimization;

public record PopulationIterationResult<TGenotype>(Population<TGenotype> Population) : IIterationResult {
  public PopulationIterationResult(IReadOnlyList<TGenotype> genotypes, IReadOnlyList<ObjectiveVector> fitnesses)
    : this(new Population<TGenotype>(genotypes, fitnesses)) { }

  public PopulationIterationResult(IReadOnlyList<ISolution<TGenotype>> solutions)
    : this(new Population<TGenotype>(solutions)) { }
}
