using HEAL.HeuristicLib.Algorithms.EvolutionStrategy;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Configuration;

public record EvolutionStrategyConfiguration {
  public int? PopulationSize { get; init; }
  public int? Children { get; init; }
  public EvolutionStrategyType? Strategy { get; init; }
  public ICreator<RealVector, RealVectorEncoding>? Creator { get; init; }
  //public ICrossover<RealVector, RealVectorEncoding>? Crossover { get; init; }
  public IMutator<RealVector, RealVectorEncoding>? Mutator { get; init; }
  public double? InitialMutationStrength { get; init; }
  public ISelector? Selector { get; init; }
  public int? RandomSeed { get; init; }
  public IInterceptor<EvolutionStrategyResult>? Interceptor { get; init; }
  public ITerminator<EvolutionStrategyResult>? Terminator { get; init; }
} 
