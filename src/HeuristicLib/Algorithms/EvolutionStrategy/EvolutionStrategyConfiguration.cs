using HEAL.HeuristicLib.Genotypes;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Algorithms.EvolutionStrategy;

public enum EvolutionStrategyType {
  Comma,
  Plus
}

public record EvolutionStrategyConfiguration<TProblem>
  where TProblem : IOptimizable<RealVector, RealVectorSearchSpace>
{
  public int? PopulationSize { get; init; }
  public int? Children { get; init; }
  public EvolutionStrategyType? Strategy { get; init; }
  public Creator<RealVector, RealVectorSearchSpace, TProblem>? Creator { get; init; }
  //public ICrossover<RealVector, RealVectorSearchSpace>? Crossover { get; init; }
  public Mutator<RealVector, RealVectorSearchSpace, TProblem>? Mutator { get; init; }
  public double? InitialMutationStrength { get; init; }
  public Selector<RealVector, RealVectorSearchSpace, TProblem>? Selector { get; init; }
  public int? RandomSeed { get; init; }
  public Interceptor<RealVector, RealVectorSearchSpace, TProblem, EvolutionStrategyIterationResult>? Interceptor { get; init; }
  public Terminator<RealVector, RealVectorSearchSpace, TProblem, EvolutionStrategyResult>? Terminator { get; init; }
} 
