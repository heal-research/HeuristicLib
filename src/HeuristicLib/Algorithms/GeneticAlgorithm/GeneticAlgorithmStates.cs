using HEAL.HeuristicLib.Core;

namespace HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;

public abstract record StartPopulation : IStartState {
  public required int Generation { get; init; }
}

public record GenotypeStartPopulation<TGenotype> : StartPopulation {
  public required TGenotype[] Population { get; init; }
}

public record PhenotypeStartPopulation<TGenotype, TPhenotype> : StartPopulation, IContinuationState {
  public required Individual<TGenotype, TPhenotype>[] Population { get; init; }
}

public abstract record EvolutionResult : IResultState {
  public required int Generation { get; init; }
  
  public required Objective Objective { get; init; }

  public int CreationCount { get; init; } = 0;
  public int DecodingCount { get; init; } = 0;
  public int EvaluationCount { get; init; } = 0;
  public int SelectionCount { get; init; } = 0;
  public int CrossoverCount { get; init; } = 0;
  public int MutationCount { get; init; } = 0;
  public int InterceptionCount { get; init; } = 0;
  
  public TimeSpan TotalDuration { get; init; } = TimeSpan.Zero;
  public TimeSpan CreationDuration { get; init; } = TimeSpan.Zero; 
  public TimeSpan DecodingDuration { get; init; } = TimeSpan.Zero;
  public TimeSpan EvaluationDuration { get; init; } = TimeSpan.Zero;
  public TimeSpan SelectionDuration { get; init; } = TimeSpan.Zero;
  public TimeSpan CrossoverDuration { get; init; } = TimeSpan.Zero;
  public TimeSpan MutationDuration { get; init; } = TimeSpan.Zero;
  public TimeSpan ReplacementDuration { get; init; } = TimeSpan.Zero;
  public TimeSpan InterceptionDuration { get; init; } = TimeSpan.Zero;
}

public record EvolutionResult<TGenotype, TPhenotype> : EvolutionResult, IContinuableResultState<PhenotypeStartPopulation<TGenotype, TPhenotype>> {
  public required Individual<TGenotype, TPhenotype>[] Population { get; init; }
  
  public PhenotypeStartPopulation<TGenotype, TPhenotype> GetNextContinuationState() => new() {
    Generation = Generation + 1,
    Population = Population
  };
}
