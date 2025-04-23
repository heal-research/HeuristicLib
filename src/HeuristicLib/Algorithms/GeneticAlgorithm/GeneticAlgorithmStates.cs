
namespace HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;

public record GeneticAlgorithmState<TGenotype> {
  public required int UsedRandomSeed { get; init; }
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
  
  public static GeneticAlgorithmOperatorMetrics Aggregate(GeneticAlgorithmOperatorMetrics a, GeneticAlgorithmOperatorMetrics b) {
    return new GeneticAlgorithmOperatorMetrics {
      Creation = a.Creation + b.Creation,
      Decoding = a.Decoding + b.Decoding,
      Evaluation = a.Evaluation + b.Evaluation,
      Selection = a.Selection + b.Selection,
      Crossover = a.Crossover + b.Crossover,
      Mutation = a.Mutation + b.Mutation,
      Replacement = a.Replacement + b.Replacement,
      Interception = a.Interception + b.Interception
    };
  }
  public static GeneticAlgorithmOperatorMetrics operator +(GeneticAlgorithmOperatorMetrics a, GeneticAlgorithmOperatorMetrics b) => Aggregate(a, b);
}

public record GeneticAlgorithmIterationResult<TGenotype> : IContinuableIterationResult<GeneticAlgorithmState<TGenotype>> {
  int IIterationResult.UsedIterationRandomSeed => UsedGenerationRandomSeed;
  public required int UsedGenerationRandomSeed { get; init; }
  
  int IIterationResult.Iteration => Generation;
  public required int Generation { get; init; }
  
  public required Objective Objective { get; init; }
  
  public required TimeSpan TotalDuration { get; init; }
  public required GeneticAlgorithmOperatorMetrics OperatorMetrics { get; init; }

  public required IReadOnlyList<EvaluatedIndividual<TGenotype>> Population { get; init; }
  
  public GeneticAlgorithmIterationResult() {
    bestSolution = new Lazy<EvaluatedIndividual<TGenotype>>(() => {
      if (Population!.Count == 0) throw new InvalidOperationException("Population is empty");
      return Population.MinBy(x => x.Fitness, Objective!.TotalOrderComparer)!;
    });
  }
  
  private readonly Lazy<EvaluatedIndividual<TGenotype>> bestSolution;
  public EvaluatedIndividual<TGenotype> BestSolution => bestSolution.Value;
  
  public GeneticAlgorithmState<TGenotype> GetNextState() => new() {
    UsedRandomSeed = UsedGenerationRandomSeed,
    Generation = Generation /*+ 1*/,
    Population = Population
  };
}

public record GeneticAlgorithmResult<TGenotype> : IAlgorithmResult {
  [Obsolete("Not necessary on result, since it can be obtained form the algorithm (parameters)")]
  public required int UsedRandomSeed { get; init; }
  public required TimeSpan TotalDuration { get; init; }
  public required int TotalGenerations { get; init; }
  public required GeneticAlgorithmOperatorMetrics OperatorMetrics { get; init; }
  public required EvaluatedIndividual<TGenotype>? BestSolution { get; init; }
}
