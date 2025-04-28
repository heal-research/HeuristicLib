
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

public record GeneticAlgorithmIterationResult<TGenotype> : ISingleObjectiveIterationResult<TGenotype>, IContinuableIterationResult<GeneticAlgorithmState<TGenotype>> {
 
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
  
  public GeneticAlgorithmState<TGenotype> GetContinuationState() => new() {
    Generation = Generation,
    Population = Population
  };

  public GeneticAlgorithmState<TGenotype> GetRestartState() => GetContinuationState() with { Generation = 0 };
}

public record GeneticAlgorithmResult<TGenotype> : ISingleObjectiveAlgorithmResult<TGenotype> {
  public required TimeSpan TotalDuration { get; init; }
  public required int TotalGenerations { get; init; }
  public required GeneticAlgorithmOperatorMetrics OperatorMetrics { get; init; }
  public required EvaluatedIndividual<TGenotype>? BestSolution { get; init; }
}
