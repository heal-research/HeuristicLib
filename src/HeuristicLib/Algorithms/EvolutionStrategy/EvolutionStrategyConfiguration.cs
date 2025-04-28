using HEAL.HeuristicLib.Encodings;
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
  public Creator<RealVector, RealVectorEncoding>? Creator { get; init; }
  //public ICrossover<RealVector, RealVectorEncoding>? Crossover { get; init; }
  public Mutator<RealVector, RealVectorEncoding>? Mutator { get; init; }
  public double? InitialMutationStrength { get; init; }
  public Selector? Selector { get; init; }
  public int? RandomSeed { get; init; }
  public Interceptor<EvolutionStrategyIterationResult>? Interceptor { get; init; }
  public Terminator<EvolutionStrategyResult>? Terminator { get; init; }
} 
