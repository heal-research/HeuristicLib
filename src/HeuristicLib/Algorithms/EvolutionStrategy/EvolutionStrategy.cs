using System.Diagnostics;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Algorithms.EvolutionStrategy;

public enum EvolutionStrategyType {
  Comma,
  Plus
}

public record EvolutionStrategyState {
  public required int UsedRandomSeed { get; init; }
  public required int Generation { get; init; }
  public required EvaluatedIndividual<RealVector>[] Population { get; init; }
  public required double MutationStrength { get; init; }
}

public record EvolutionStrategyOperatorMetrics {
  public OperatorMetric Creation { get; init; } = OperatorMetric.Zero;
  public OperatorMetric Decoding { get; init; } = OperatorMetric.Zero;
  public OperatorMetric Evaluation { get; init; } = OperatorMetric.Zero;
  public OperatorMetric Selection { get; init; } = OperatorMetric.Zero;
  //public OperatorMetric Crossover { get; init; } = OperatorMetric.Zero;
  public OperatorMetric Mutation { get; init; } = OperatorMetric.Zero;
  public OperatorMetric Replacement { get; init; } = OperatorMetric.Zero;
  public OperatorMetric Interception { get; init; } = OperatorMetric.Zero;
  
  public static EvolutionStrategyOperatorMetrics Aggregate(EvolutionStrategyOperatorMetrics a, EvolutionStrategyOperatorMetrics b) {
    return new EvolutionStrategyOperatorMetrics {
      Creation = a.Creation + b.Creation,
      Decoding = a.Decoding + b.Decoding,
      Evaluation = a.Evaluation + b.Evaluation,
      Selection = a.Selection + b.Selection,
      //Crossover = a.Crossover + b.Crossover,
      Mutation = a.Mutation + b.Mutation,
      Replacement = a.Replacement + b.Replacement,
      Interception = a.Interception + b.Interception
    };
  }
  public static EvolutionStrategyOperatorMetrics operator +(EvolutionStrategyOperatorMetrics a, EvolutionStrategyOperatorMetrics b) => Aggregate(a, b);
}


public record EvolutionStrategyIterationResult : IContinuableIterationResult<EvolutionStrategyState> {
  int IIterationResult.UsedIterationRandomSeed => UsedGenerationRandomSeed;
  public required int UsedGenerationRandomSeed { get; init; }
  
  int IIterationResult.Iteration => Generation;
  public required int Generation { get; init; }
  
  public required Objective Objective { get; init; }
  
  public required double MutationStrength { get; init; }
  
  public required TimeSpan TotalDuration { get; init; }
  public required EvolutionStrategyOperatorMetrics OperatorMetrics { get; init; }
  
  public required IReadOnlyList<EvaluatedIndividual<RealVector>> Population { get; init; }
  
  public EvolutionStrategyIterationResult() {
    bestSolution = new Lazy<EvaluatedIndividual<RealVector>>(() => {
      if (Population!.Count == 0) throw new InvalidOperationException("Population is empty");
      return Population.MinBy(x => x.Fitness, Objective!.TotalOrderComparer)!;
    });
  }
  
  private readonly Lazy<EvaluatedIndividual<RealVector>> bestSolution;
  public EvaluatedIndividual<RealVector> BestSolution => bestSolution.Value;
  
  public EvolutionStrategyState GetNextState() => new() {
    UsedRandomSeed = UsedGenerationRandomSeed,
    Generation = Generation /*+ 1*/,
    MutationStrength = MutationStrength,
    Population = Population.Select(i => new EvaluatedIndividual<RealVector>(i.Genotype, i.Fitness)).ToArray()
  };
}

public record EvolutionStrategyResult : IAlgorithmResult {
  [Obsolete("Not necessary on result, since it can be obtained form the algorithm (parameters)")]
  public required int UsedRandomSeed { get; init; }
  public required TimeSpan TotalDuration { get; init; }
  public required int TotalGenerations { get; init; }
  public required EvolutionStrategyOperatorMetrics OperatorMetrics { get; init; }
  public required EvaluatedIndividual<RealVector>? BestSolution { get; init; }
}


public class EvolutionStrategy
  : IterativeAlgorithm<RealVector, RealVectorEncoding, EvolutionStrategyState, EvolutionStrategyIterationResult, EvolutionStrategyResult>
{
  public int PopulationSize { get; }
  public int Children { get; }
  public EvolutionStrategyType Strategy { get; }
  public ICreator<RealVector, RealVectorEncoding> Creator { get; }
  //public ICrossover<RealVector, RealVectorEncoding>? Crossover { get; }
  public IMutator<RealVector, RealVectorEncoding> Mutator { get; }
  public double InitialMutationStrength { get; }
  public ISelector Selector { get; }
  public int RandomSeed { get; }
  public IInterceptor<EvolutionStrategyIterationResult> Interceptor { get; }

  public EvolutionStrategy(
    int populationSize,
    int children,
    EvolutionStrategyType strategy,
    ICreator<RealVector, RealVectorEncoding> creator,
    ICrossover<RealVector, RealVectorEncoding> crossover,
    IMutator<RealVector, RealVectorEncoding> mutator,
    double initialMutationStrength,
    ISelector selector,
    int randomSeed,
    ITerminator<EvolutionStrategyIterationResult> terminator,
    IInterceptor<EvolutionStrategyIterationResult>? interceptor = null) 
  : base(terminator) {
    PopulationSize = populationSize;
    Children = children;
    Strategy = strategy;
    Creator = creator;
    //Crossover = crossover;
    Mutator = mutator;
    InitialMutationStrength = initialMutationStrength;
    Selector = selector;
    RandomSeed = randomSeed;
    Interceptor = interceptor ?? Interceptors.Identity<EvolutionStrategyIterationResult>();
}
  
  // public override EvaluatedIndividual<RealVector, TPhenotype> Solve<TPhenotype>(IEncodedProblem<TPhenotype, RealVector, RealVectorEncoding> problem, EvolutionStrategyState? initialState = null) {
  //   var lastState = (EvolutionStrategyResult<TPhenotype>)Execute(problem, initialState);
  //   return lastState.BestPhenotypeSolution;
  // }
  //
  // public override IEnumerable<EvaluatedIndividual<RealVector, TPhenotype>> SolveStreaming<TPhenotype>(IEncodedProblem<TPhenotype, RealVector, RealVectorEncoding> problem, EvolutionStrategyState? initialState = null) {
  //   foreach (var state in ExecuteStreaming(problem, initialState).Cast<EvolutionStrategyResult<TPhenotype>>()) {
  //     yield return state.BestPhenotypeSolution;
  //   }
  // }
  
  public override EvolutionStrategyResult Execute<TPhenotype>(IEncodedProblem<TPhenotype, RealVector, RealVectorEncoding> problem, EvolutionStrategyState? initialState = null) {
    EvaluatedIndividual<RealVector>? bestSolution = null;
    var comparer = problem.Objective.TotalOrderComparer;
    
    int totalGenerations = 0;
    TimeSpan totalDuration = TimeSpan.Zero;
    var totalMetrics = new EvolutionStrategyOperatorMetrics();
    
    foreach (var result in ExecuteStreaming(problem, initialState)) {
      if (bestSolution is null || comparer.Compare(bestSolution.Fitness, result.BestSolution.Fitness) < 0) 
        bestSolution = result.BestSolution;
      totalGenerations += 1;
      totalDuration += result.TotalDuration;
      totalMetrics += result.OperatorMetrics;
    }
    
    return new EvolutionStrategyResult() {
      UsedRandomSeed = RandomSeed,
      TotalGenerations = totalGenerations,
      TotalDuration = totalDuration,
      OperatorMetrics = totalMetrics,
      BestSolution = bestSolution
    };
  }
  
  protected override EvolutionStrategyIterationResult ExecuteInitialization<TPhenotype>(IEncodedProblem<TPhenotype, RealVector, RealVectorEncoding> problem) {
    var start = Stopwatch.GetTimestamp();

    var random = new SystemRandomNumberGenerator(RandomSeed);
    
    //var givenPopulation = startState?.Population ?? [];
    //int remainingCount = PopulationSize - givenPopulation.Length;

    var startCreating = Stopwatch.GetTimestamp();
    //var newPopulation = Enumerable.Range(0, remainingCount).Select(i => Creator.Create(problem.Encoding, random)).ToArray();
    var newPopulation = Enumerable.Range(0, PopulationSize).Select(i => Creator.Create(problem.Encoding, random)).ToArray();
    var endCreating = Stopwatch.GetTimestamp();
    
    //var genotypePopulation = givenPopulation.Concat(newPopulation).Take(PopulationSize).ToArray();
    var genotypePopulation = newPopulation;
    
    var startDecoding = Stopwatch.GetTimestamp();
    var phenotypePopulation = genotypePopulation.Select(genotype => problem.Decoder.Decode(genotype)).ToArray();
    var endDecoding = Stopwatch.GetTimestamp();
    
    var startEvaluating = Stopwatch.GetTimestamp();
    var fitnesses = phenotypePopulation.Select(phenotype => problem.Evaluator.Evaluate(phenotype)).ToArray();
    var endEvaluating = Stopwatch.GetTimestamp();

    var population = Population.From(genotypePopulation, phenotypePopulation, fitnesses);

    var endBeforeInterceptor = Stopwatch.GetTimestamp();
    
    var result = new EvolutionStrategyIterationResult() {
      UsedGenerationRandomSeed = RandomSeed,
      Generation = 0,
      MutationStrength = InitialMutationStrength,
      Objective = problem.Objective,
      Population = population,
      TotalDuration = Stopwatch.GetElapsedTime(start, endBeforeInterceptor),
      OperatorMetrics = new () {
        Creation = new(genotypePopulation.Length, Stopwatch.GetElapsedTime(startCreating, endCreating)),
        Decoding = new(phenotypePopulation.Length, Stopwatch.GetElapsedTime(startDecoding, endDecoding)),
        Evaluation = new(fitnesses.Length, Stopwatch.GetElapsedTime(startEvaluating, endEvaluating)),
      }
    };
    
    var interceptorStart = Stopwatch.GetTimestamp();
    var interceptedResult = Interceptor.Transform(result);
    var interceptorEnd = Stopwatch.GetTimestamp();

    if (interceptedResult == result) return result;

    var end = Stopwatch.GetTimestamp();
    
    return interceptedResult with {
      TotalDuration = Stopwatch.GetElapsedTime(start, end),
      OperatorMetrics = interceptedResult.OperatorMetrics with {
        Interception = new(1, Stopwatch.GetElapsedTime(interceptorStart, interceptorEnd))
      } 
    };
  }
  
  protected override EvolutionStrategyIterationResult ExecuteIteration<TPhenotype>(IEncodedProblem<TPhenotype, RealVector, RealVectorEncoding> problem, EvolutionStrategyState state) {
    var start = Stopwatch.GetTimestamp();

    int newRandomSeed = SeedSequence.GetSeed(RandomSeed, state.Generation);
    var random = new SystemRandomNumberGenerator(newRandomSeed);
    
    var oldPopulation = state.Population;
    
    var startSelection = Stopwatch.GetTimestamp();
    var randomSelector = new RandomSelector();
    var parents = randomSelector.Select(oldPopulation, problem.Objective, PopulationSize, random).ToList();
    var endSelection = Stopwatch.GetTimestamp();
     
    var genotypePopulation = new RealVector[parents.Count];
    var startMutation = Stopwatch.GetTimestamp();
    for (int i = 0; i < parents.Count; i += 2) {
      var parent = parents[i];
      var child = Mutator.Mutate(parent.Genotype, problem.Encoding, random);
      genotypePopulation[i / 2] = child;
    }
    var endMutation = Stopwatch.GetTimestamp();
    
    // ToDo: optional crossover
    
    var startDecoding = Stopwatch.GetTimestamp();
    var phenotypePopulation = genotypePopulation.Select(genotype => problem.Decoder.Decode(genotype)).ToArray();
    var endDecoding = Stopwatch.GetTimestamp();
    
    var startEvaluation = Stopwatch.GetTimestamp();
    var fitnesses = phenotypePopulation.Select(phenotype => problem.Evaluator.Evaluate(phenotype)).ToArray();
    var endEvaluation = Stopwatch.GetTimestamp();
    
    // timing the adaption check
    int successfulOffspring = 0;
    for (int i = 0; i < fitnesses.Length; i++) {
      if (fitnesses[i].CompareTo(parents[i].Fitness, problem.Objective) == DominanceRelation.Dominates) {
        successfulOffspring++;
      }
    }
    double successRate = (double)successfulOffspring / genotypePopulation.Length;
    double newMutationStrength = successRate switch {
      > 0.2 => state.MutationStrength * 1.5,
      < 0.2 => state.MutationStrength / 1.5,
      _ => state.MutationStrength
    };
    
    var population = Population.From(genotypePopulation, phenotypePopulation, fitnesses);
    
    var startReplacement = Stopwatch.GetTimestamp();
    IReplacer replacer = Strategy switch {
      EvolutionStrategyType.Comma => new ElitismReplacer(0),
      EvolutionStrategyType.Plus => new PlusSelectionReplacer(),
      _ => throw new InvalidOperationException($"Unknown strategy {Strategy}")
    };
    var newPopulation = replacer.Replace(oldPopulation, population, problem.Objective, random);
    var endReplacement = Stopwatch.GetTimestamp();
    
    var endBeforeInterceptor = Stopwatch.GetTimestamp();

    var result = new EvolutionStrategyIterationResult() {
      UsedGenerationRandomSeed = newRandomSeed,
      Generation = state.Generation + 1,
      MutationStrength = newMutationStrength,
      Objective = problem.Objective,
      Population = newPopulation,
      TotalDuration = Stopwatch.GetElapsedTime(start, endBeforeInterceptor),
      OperatorMetrics = new() {
        Selection = new(parents.Count, Stopwatch.GetElapsedTime(startSelection, endSelection)),
        Mutation = new(genotypePopulation.Length, Stopwatch.GetElapsedTime(startMutation, endMutation)),
        Decoding = new(phenotypePopulation.Length, Stopwatch.GetElapsedTime(startDecoding, endDecoding)),
        Evaluation = new (fitnesses.Length, Stopwatch.GetElapsedTime(startEvaluation, endEvaluation)),
        Replacement = new (1, Stopwatch.GetElapsedTime(startReplacement, endReplacement)),
      }
    };
    
    var interceptorStart = Stopwatch.GetTimestamp();
    var interceptedResult = Interceptor.Transform(result);
    var interceptorEnd = Stopwatch.GetTimestamp();
    if (interceptedResult == result) return result;
    
    var end = Stopwatch.GetTimestamp();
    
    return interceptedResult with {
      TotalDuration = Stopwatch.GetElapsedTime(start, end),
      OperatorMetrics = interceptedResult.OperatorMetrics with {
        Interception = new(1, Stopwatch.GetElapsedTime(interceptorStart, interceptorEnd))
      }
    };
  }
  
  
  // public override ResultStream<EvolutionStrategyPopulationState> CreateExecutionStream(EvolutionStrategyPopulationState? initialState = null) {
  //   return new ResultStream<EvolutionStrategyPopulationState>(InternalCreateExecutionStream(initialState));
  // }
  //
  // private IEnumerable<EvolutionStrategyPopulationState> InternalCreateExecutionStream(EvolutionStrategyPopulationState? initialState) {
  //   var random = RandomSource.CreateRandomNumberGenerator();
  //   
  //   EvolutionStrategyPopulationState currentState;
  //   if (initialState is null) {
  //     var initialPopulation = InitializePopulation();
  //     var evaluatedInitialPopulation = Evaluator.Evaluate(initialPopulation);
  //     yield return currentState = new EvolutionStrategyPopulationState { Objective = Objective, MutationStrength = InitialMutationStrength, Population = evaluatedInitialPopulation }; 
  //   } else {
  //     currentState = initialState;
  //   }
  //   
  //   while (Terminator?.ShouldContinue(currentState) ?? true) {
  //     var (offspringPopulation, successfulOffspring) = EvolvePopulation(currentState.Population, currentState.MutationStrength, random);
  //     var evaluatedOffspring = Evaluator.Evaluate(offspringPopulation);
  //
  //     var newPopulation = Strategy switch {
  //       EvolutionStrategyType.Comma => evaluatedOffspring,
  //       EvolutionStrategyType.Plus => CombinePopulations(currentState.Population, evaluatedOffspring),
  //       _ => throw new NotImplementedException("Unknown strategy")
  //     };
  //     
  //     double successRate = (double)successfulOffspring / offspringPopulation.Length;
  //     double newMutationStrength = successRate switch {
  //       > 0.2 => currentState.MutationStrength * 1.5,
  //       < 0.2 => currentState.MutationStrength / 1.5,
  //       _ => currentState.MutationStrength
  //     };
  //
  //     yield return currentState = currentState.Next() with { MutationStrength = newMutationStrength, Population = newPopulation };
  //   }
  // }
  //
  // private RealVector[] InitializePopulation() {
  //   var population = new RealVector[PopulationSize];
  //   for (int i = 0; i < PopulationSize; i++) {
  //     population[i] = Creator.Create();
  //   }
  //   return population;
  // }
  //
  // private (RealVector[], int successfulOffspring) EvolvePopulation(Solution<RealVector>[] population, double mutationStrength, IRandomNumberGenerator random) {
  //   var offspringPopulation = new RealVector[Children];
  //   for (int i = 0; i < Children; i++) {
  //     var parent = population[random.Integer(PopulationSize)].Phenotype;
  //     // var offspring = Mutator is IAdaptableMutator<RealVector> adaptableMutator 
  //     //   ? adaptableMutator.Mutate(parent, mutationStrength) 
  //     //   : Mutator.Mutate(parent);
  //     var offspring = Mutator.Mutate(parent);
  //     offspringPopulation[i] = offspring;
  //   }
  //   return (offspringPopulation, random.Integer(Children, Children * 10));
  //   // actually calculate success rate
  //   // would require to evaluate individuals immediately or to store the parent for later comparison after child evaluation
  // }
  //
  // private Solution<RealVector>[] CombinePopulations(Solution<RealVector>[] parents, Solution<RealVector>[] offspring) {
  //   return parents.Concat(offspring)
  //     .Take(PopulationSize)
  //     .ToArray();
  // }
}
