using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Algorithms;

public record PopulationAlgorithmState<TGenotype>(Population<TGenotype> Population) : IAlgorithmState {
  public PopulationAlgorithmState(IReadOnlyList<TGenotype> genotypes, IReadOnlyList<ObjectiveVector> fitnesses)
    : this(new Population<TGenotype>(genotypes, fitnesses)) { }

  public PopulationAlgorithmState(IReadOnlyList<ISolution<TGenotype>> solutions)
    : this(new Population<TGenotype>(solutions)) { }
}
