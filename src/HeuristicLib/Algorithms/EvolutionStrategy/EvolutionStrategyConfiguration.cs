using HEAL.HeuristicLib.Genotypes;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Algorithms.EvolutionStrategy;

public enum EvolutionStrategyType {
  Comma,
  Plus
}

public record EvolutionStrategyConfiguration {
  public int? PopulationSize { get; init; }
  public int? Children { get; init; }
  public EvolutionStrategyType? Strategy { get; init; }
  public Creator<RealVector, RealVectorSearchSpace>? Creator { get; init; }
  //public ICrossover<RealVector, RealVectorSearchSpace>? Crossover { get; init; }
  public Mutator<RealVector, RealVectorSearchSpace>? Mutator { get; init; }
  public double? InitialMutationStrength { get; init; }
  public Selector? Selector { get; init; }
  public int? RandomSeed { get; init; }
  public Interceptor<EvolutionStrategyIterationResult>? Interceptor { get; init; }
  public Terminator<EvolutionStrategyResult>? Terminator { get; init; }
} 
