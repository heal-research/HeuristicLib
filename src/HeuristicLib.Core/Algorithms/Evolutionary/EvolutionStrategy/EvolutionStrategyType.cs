namespace HEAL.HeuristicLib.Algorithms.Evolutionary.EvolutionStrategy;

public enum EvolutionStrategyType
{
  Comma,
  Plus
}

// public record EvolutionStrategyConfiguration<TProblemContext>
//   where TProblemContext : IOptimizable<RealVector>
// {
//   public int? PopulationSize { get; init; }
//   public int? NoChildren { get; init; }
//   public EvolutionStrategyType? Strategy { get; init; }
//   public Creator<RealVector, TProblemContext>? Creator { get; init; }
//   //public ICrossover<RealVector, RealVectorSearchSpace>? Crossover { get; init; }
//   public Mutator<RealVector, TProblemContext>? Mutator { get; init; }
//   public double? InitialMutationStrength { get; init; }
//   public Selector<TProblemContext>? Selector { get; init; }
//   public int? RandomSeed { get; init; }
//   public Interceptor<TProblemContext, EvolutionStrategyIterationResult>? Interceptor { get; init; }
//   public Terminator<TProblemContext, EvolutionStrategyResult>? Terminator { get; init; }
// } 
