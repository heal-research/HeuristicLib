
namespace HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;

public record GeneticAlgorithmState<TGenotype> {
  public required int Generation { get; init; }
  public required IReadOnlyList<EvaluatedIndividual<TGenotype>> Population { get; init; }
}

public record GeneticAlgorithmOperatorMetrics {
  public OperatorMetric Creation { get; init; } = OperatorMetric.Zero;
  public OperatorMetric Decoding { get; init; } = OperatorMetric.Zero;
  public OperatorMetric Evaluation { get; init; } = OperatorMetric.Zero;
  public OperatorMetric Selection { get; init; } = OperatorMetric.Zero;
  public OperatorMetric Crossover { get; init; } = OperatorMetric.Zero;
  public OperatorMetric Mutation { get; init; } = OperatorMetric.Zero;
  public OperatorMetric Replacement { get; init; } = OperatorMetric.Zero;
  public OperatorMetric Interception { get; init; } = OperatorMetric.Zero;
  
  public static GeneticAlgorithmOperatorMetrics Aggregate(GeneticAlgorithmOperatorMetrics left, GeneticAlgorithmOperatorMetrics right) {
    return new GeneticAlgorithmOperatorMetrics {
      Creation = left.Creation + right.Creation,
      Decoding = left.Decoding + right.Decoding,
      Evaluation = left.Evaluation + right.Evaluation,
      Selection = left.Selection + right.Selection,
      Crossover = left.Crossover + right.Crossover,
      Mutation = left.Mutation + right.Mutation,
      Replacement = left.Replacement + right.Replacement,
      Interception = left.Interception + right.Interception
    };
  }
  public static GeneticAlgorithmOperatorMetrics operator +(GeneticAlgorithmOperatorMetrics left, GeneticAlgorithmOperatorMetrics right) => Aggregate(left, right);
}

public record GeneticAlgorithmIterationResult<TGenotype> {
  public required int Generation { get; init; }
  public required TimeSpan Duration { get; init; }
  public required GeneticAlgorithmOperatorMetrics OperatorMetrics { get; init; }
  public required Objective Objective { get; init; }
  public required IReadOnlyList<EvaluatedIndividual<TGenotype>> Population { get; init; }
}

public record GeneticAlgorithmResult<TGenotype> : ISingleObjectiveAlgorithmResult<TGenotype>, IContinuableAlgorithmResult<GeneticAlgorithmState<TGenotype>> {
  int IAlgorithmResult.CurrentIteration => CurrentGeneration;
  int IAlgorithmResult.TotalIterations => TotalGenerations;
  
  public required int CurrentGeneration { get; init; }
  public required int TotalGenerations { get; init; }
  
  public required TimeSpan CurrentDuration { get; init; }
  public required TimeSpan TotalDuration { get; init; }
  
  public required GeneticAlgorithmOperatorMetrics CurrentOperatorMetrics { get; init; }
  public required GeneticAlgorithmOperatorMetrics TotalOperatorMetrics { get; init; }

  public required Objective Objective { get; init; }
  
  public required IReadOnlyList<EvaluatedIndividual<TGenotype>> CurrentPopulation { get; init; }
  
  public GeneticAlgorithmResult() {
    currentBestSolution = new Lazy<EvaluatedIndividual<TGenotype>>(() => {
      if (CurrentPopulation!.Count == 0) throw new InvalidOperationException("Population is empty");
      return CurrentPopulation.MinBy(x => x.Fitness, Objective!.TotalOrderComparer)!;
    });
  }
  
  private readonly Lazy<EvaluatedIndividual<TGenotype>> currentBestSolution;
  public EvaluatedIndividual<TGenotype> CurrentBestSolution => currentBestSolution.Value;
  
  public GeneticAlgorithmState<TGenotype> GetContinuationState() => new() {
    Generation = CurrentGeneration,
    Population = CurrentPopulation
  };

  public GeneticAlgorithmState<TGenotype> GetRestartState() => GetContinuationState() with { Generation = 0 };
}

// public record GeneticAlgorithmResult<TGenotype> : ISingleObjectiveAlgorithmResult<TGenotype> {
//   public required TimeSpan TotalDuration { get; init; }
//   public required int TotalGenerations { get; init; }
//   public required GeneticAlgorithmOperatorMetrics OperatorMetrics { get; init; }
//   public required EvaluatedIndividual<TGenotype>? BestSolution { get; init; }
// }
