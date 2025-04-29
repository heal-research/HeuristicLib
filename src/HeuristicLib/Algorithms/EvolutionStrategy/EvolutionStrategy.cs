using System.Diagnostics;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Algorithms.EvolutionStrategy;

public record EvolutionStrategyState {
  public required int Generation { get; init; }
  public required IReadOnlyList<EvaluatedIndividual<RealVector>> Population { get; init; }
  public required double MutationStrength { get; init; }
}

public record EvolutionStrategyOperatorMetrics {
  public OperatorMetric Creation { get; init; } = OperatorMetric.Zero;
  // public OperatorMetric Decoding { get; init; } = OperatorMetric.Zero;
  public OperatorMetric Evaluation { get; init; } = OperatorMetric.Zero;
  public OperatorMetric Selection { get; init; } = OperatorMetric.Zero;
  //public OperatorMetric Crossover { get; init; } = OperatorMetric.Zero;
  public OperatorMetric Mutation { get; init; } = OperatorMetric.Zero;
  public OperatorMetric Replacement { get; init; } = OperatorMetric.Zero;
  public OperatorMetric Interception { get; init; } = OperatorMetric.Zero;
  
  public static EvolutionStrategyOperatorMetrics Aggregate(EvolutionStrategyOperatorMetrics left, EvolutionStrategyOperatorMetrics right) {
    return new EvolutionStrategyOperatorMetrics {
      Creation = left.Creation + right.Creation,
      // Decoding = left.Decoding + right.Decoding,
      Evaluation = left.Evaluation + right.Evaluation,
      Selection = left.Selection + right.Selection,
      //Crossover = a.Crossover + b.Crossover,
      Mutation = left.Mutation + right.Mutation,
      Replacement = left.Replacement + right.Replacement,
      Interception = left.Interception + right.Interception
    };
  }
  public static EvolutionStrategyOperatorMetrics operator +(EvolutionStrategyOperatorMetrics left, EvolutionStrategyOperatorMetrics right) => Aggregate(left, right);
}

public record EvolutionStrategyIterationResult {
  public required int Generation { get; init; }
  public required TimeSpan Duration { get; init; }
  public required EvolutionStrategyOperatorMetrics OperatorMetrics { get; init; }
  public required double MutationStrength { get; init; }
  public required Objective Objective { get; init; }
  public required IReadOnlyList<EvaluatedIndividual<RealVector>> Population { get; init; }
}

public record EvolutionStrategyResult : ISingleObjectiveAlgorithmResult<RealVector>, IContinuableAlgorithmResult<EvolutionStrategyState> {
  int IAlgorithmResult.CurrentIteration => CurrentGeneration;
  int IAlgorithmResult.TotalIterations => TotalGenerations;
  
  public required int CurrentGeneration { get; init; }
  public required int TotalGenerations { get; init; }
  
  public required TimeSpan CurrentDuration { get; init; }
  public required TimeSpan TotalDuration { get; init; }
  
  public required double CurrentMutationStrength { get; init; }
  
  public required EvolutionStrategyOperatorMetrics CurrentOperatorMetrics { get; init; }
  public required EvolutionStrategyOperatorMetrics TotalOperatorMetrics { get; init; }

  public required Objective Objective { get; init; }
  
  public required IReadOnlyList<EvaluatedIndividual<RealVector>> CurrentPopulation { get; init; }
  
  public EvolutionStrategyResult() {
    currentBestSolution = new Lazy<EvaluatedIndividual<RealVector>>(() => {
      if (CurrentPopulation!.Count == 0) throw new InvalidOperationException("Population is empty");
      return CurrentPopulation.MinBy(x => x.Fitness, Objective!.TotalOrderComparer)!;
    });
  }
  
  private readonly Lazy<EvaluatedIndividual<RealVector>> currentBestSolution;
  public EvaluatedIndividual<RealVector> CurrentBestSolution => currentBestSolution.Value;
  
  public EvolutionStrategyState GetContinuationState() => new() {
    Generation = CurrentGeneration,
    MutationStrength = CurrentMutationStrength,
    Population = CurrentPopulation
  };

  public EvolutionStrategyState GetRestartState() => GetContinuationState() with { Generation = 0 };
}

public record class EvolutionStrategy
  : IterativeAlgorithm<RealVector, RealVectorEncoding, EvolutionStrategyState, EvolutionStrategyIterationResult, EvolutionStrategyResult> {
  public int PopulationSize { get; init;  }
  public int Children { get; init;  }
  public EvolutionStrategyType Strategy { get; init; }

  public Creator<RealVector, RealVectorEncoding> Creator { get; init; }

  //public ICrossover<RealVector, RealVectorEncoding>? Crossover { get; }
  public Mutator<RealVector, RealVectorEncoding> Mutator { get; }
  public double InitialMutationStrength { get; init;  }
  public Selector Selector { get; init;  }
  public int RandomSeed { get; init;  }
  public Interceptor<EvolutionStrategyIterationResult>? Interceptor { get; init; }

  public EvolutionStrategy(
    int populationSize,
    int children,
    EvolutionStrategyType strategy,
    Creator<RealVector, RealVectorEncoding> creator,
    //ICrossover<RealVector, RealVectorEncoding> crossover,
    Mutator<RealVector, RealVectorEncoding> mutator,
    double initialMutationStrength,
    Selector selector,
    int randomSeed,
    Terminator<EvolutionStrategyResult> terminator,
    Interceptor<EvolutionStrategyIterationResult>? interceptor = null)
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
    Interceptor = interceptor;
  }

  public override EvolutionStrategyInstance CreateInstance() {
    return new EvolutionStrategyInstance(this);
  }
}

public class EvolutionStrategyInstance
  : IterativeAlgorithmInstance<RealVector, RealVectorEncoding, EvolutionStrategyState, EvolutionStrategyIterationResult, EvolutionStrategyResult, EvolutionStrategy> 
{
  public IRandomNumberGenerator Random { get; }
  public ICreatorInstance<RealVector, RealVectorEncoding> Creator { get; }
  //public ICrossoverInstance<RealVector, RealVectorEncoding>? Crossover { get; }
  public IMutatorInstance<RealVector, RealVectorEncoding> Mutator { get; }
  public ISelectorInstance Selector { get; }
  public IInterceptorInstance<EvolutionStrategyIterationResult>? Interceptor { get; }
  
  public EvolutionStrategyInstance(EvolutionStrategy parameters) : base(parameters) {
    Random = new SystemRandomNumberGenerator(parameters.RandomSeed);
    Creator = parameters.Creator.CreateInstance();
    //Crossover = parameters.Crossover?.CreateInstance();
    Mutator = parameters.Mutator.CreateInstance();
    Selector = parameters.Selector.CreateInstance();
    Interceptor = parameters.Interceptor?.CreateInstance();
  }
  
  // public override EvolutionStrategyResult Execute<TPhenotype>(IEncodedProblem<TPhenotype, RealVector, RealVectorEncoding> problem, EvolutionStrategyState? initialState = null) {
  //   EvaluatedIndividual<RealVector>? bestSolution = null;
  //   var comparer = problem.Objective.TotalOrderComparer;
  //   
  //   int totalGenerations = 0;
  //   TimeSpan totalDuration = TimeSpan.Zero;
  //   var totalMetrics = new EvolutionStrategyOperatorMetrics();
  //   
  //   foreach (var result in ExecuteStreaming(problem, initialState)) {
  //     if (bestSolution is null || comparer.Compare(bestSolution.Fitness, result.BestSolution.Fitness) < 0) 
  //       bestSolution = result.BestSolution;
  //     totalGenerations += 1;
  //     totalDuration += result.TotalDuration;
  //     totalMetrics += result.OperatorMetrics;
  //   }
  //   
  //   return new EvolutionStrategyResult() {
  //     TotalGenerations = totalGenerations,
  //     TotalDuration = totalDuration,
  //     OperatorMetrics = totalMetrics,
  //     BestSolution = bestSolution
  //   };
  // }
  
  protected override EvolutionStrategyIterationResult ExecuteInitialization(IOptimizable<RealVector, RealVectorEncoding> optimizable) {
    var start = Stopwatch.GetTimestamp();

    //var random = new SystemRandomNumberGenerator(Algorithm.RandomSeed);
    
    var startCreating = Stopwatch.GetTimestamp();
    var genotypes = Enumerable.Range(0, Parameters.PopulationSize).Select(i => Creator.Create(optimizable.SearchSpace, Random)).ToArray();
    var endCreating = Stopwatch.GetTimestamp();
    
    // var genotypePopulation = newPopulation;
    
    // var startDecoding = Stopwatch.GetTimestamp();
    // var phenotypePopulation = genotypePopulation.Select(genotype => optimizable.Decoder.Decode(genotype)).ToArray();
    // var endDecoding = Stopwatch.GetTimestamp();
    
    var startEvaluating = Stopwatch.GetTimestamp();
    var fitnesses = genotypes.Select(genotype => optimizable.Evaluate(genotype)).ToArray();
    var endEvaluating = Stopwatch.GetTimestamp();

    var population = Population.From(genotypes, /*phenotypePopulation,*/ fitnesses);

    var endBeforeInterceptor = Stopwatch.GetTimestamp();
    
    var result = new EvolutionStrategyIterationResult() {
      Generation = 0,
      Duration = Stopwatch.GetElapsedTime(start, endBeforeInterceptor),
      MutationStrength = Parameters.InitialMutationStrength,
      Objective = optimizable.Objective,
      Population = population,
      OperatorMetrics = new () {
        Creation = new(genotypes.Length, Stopwatch.GetElapsedTime(startCreating, endCreating)),
        // Decoding = new(phenotypePopulation.Length, Stopwatch.GetElapsedTime(startDecoding, endDecoding)),
        Evaluation = new(fitnesses.Length, Stopwatch.GetElapsedTime(startEvaluating, endEvaluating)),
      }
    };
    
    if (Interceptor is null) return result;
    
    var interceptorStart = Stopwatch.GetTimestamp();
    var interceptedResult = Interceptor.Transform(result);
    var interceptorEnd = Stopwatch.GetTimestamp();

    if (interceptedResult == result) return result;

    var end = Stopwatch.GetTimestamp();
    
    return interceptedResult with {
      Duration = Stopwatch.GetElapsedTime(start, end),
      OperatorMetrics = interceptedResult.OperatorMetrics with {
        Interception = new(1, Stopwatch.GetElapsedTime(interceptorStart, interceptorEnd))
      } 
    };
  }
  
  protected override EvolutionStrategyIterationResult ExecuteIteration(IOptimizable<RealVector, RealVectorEncoding> optimizable, EvolutionStrategyState state) {
    var start = Stopwatch.GetTimestamp();

    // int newRandomSeed = SeedSequence.GetSeed(Algorithm.RandomSeed, state.Generation);
    // var random = new SystemRandomNumberGenerator(newRandomSeed);
    
    var oldPopulation = state.Population;
    
    var startSelection = Stopwatch.GetTimestamp();
    var randomSelector = new RandomSelector().CreateInstance(); // improve
    var parents = randomSelector.Select(oldPopulation, optimizable.Objective, Parameters.PopulationSize, Random).ToList();
    var endSelection = Stopwatch.GetTimestamp();
     
    var genotypes = new RealVector[parents.Count];
    var startMutation = Stopwatch.GetTimestamp();
    for (int i = 0; i < parents.Count; i += 2) {
      var parent = parents[i];
      var child = Mutator.Mutate(parent.Genotype, optimizable.SearchSpace, Random);
      genotypes[i / 2] = child;
    }
    var endMutation = Stopwatch.GetTimestamp();
    
    // ToDo: optional crossover
    
    // var startDecoding = Stopwatch.GetTimestamp();
    // var phenotypePopulation = genotypePopulation.Select(genotype => optimizable.Decoder.Decode(genotype)).ToArray();
    // var endDecoding = Stopwatch.GetTimestamp();
    
    var startEvaluation = Stopwatch.GetTimestamp();
    var fitnesses = genotypes.Select(genotype => optimizable.Evaluate(genotype)).ToArray();
    var endEvaluation = Stopwatch.GetTimestamp();
    
    // timing the adaption check
    int successfulOffspring = 0;
    for (int i = 0; i < fitnesses.Length; i++) {
      if (fitnesses[i].CompareTo(parents[i].Fitness, optimizable.Objective) == DominanceRelation.Dominates) {
        successfulOffspring++;
      }
    }
    double successRate = (double)successfulOffspring / genotypes.Length;
    double newMutationStrength = successRate switch {
      > 0.2 => state.MutationStrength * 1.5,
      < 0.2 => state.MutationStrength / 1.5,
      _ => state.MutationStrength
    };
    
    var population = Population.From(genotypes, /*phenotypePopulation,*/ fitnesses);
    
    var startReplacement = Stopwatch.GetTimestamp();
    Replacer replacer = Parameters.Strategy switch {
      EvolutionStrategyType.Comma => new ElitismReplacer(0),
      EvolutionStrategyType.Plus => new PlusSelectionReplacer(),
      _ => throw new InvalidOperationException($"Unknown strategy {Parameters.Strategy}")
    };
    var newPopulation = replacer.CreateInstance().Replace(oldPopulation, population, optimizable.Objective, Random);
    var endReplacement = Stopwatch.GetTimestamp();
    
    var endBeforeInterceptor = Stopwatch.GetTimestamp();

    var result = new EvolutionStrategyIterationResult() {
      Generation = state.Generation + 1,
      Duration = Stopwatch.GetElapsedTime(start, endBeforeInterceptor),
      MutationStrength = newMutationStrength,
      Objective = optimizable.Objective,
      Population = newPopulation,
      OperatorMetrics = new() {
        Selection = new(parents.Count, Stopwatch.GetElapsedTime(startSelection, endSelection)),
        Mutation = new(genotypes.Length, Stopwatch.GetElapsedTime(startMutation, endMutation)),
        // Decoding = new(phenotypePopulation.Length, Stopwatch.GetElapsedTime(startDecoding, endDecoding)),
        Evaluation = new (fitnesses.Length, Stopwatch.GetElapsedTime(startEvaluation, endEvaluation)),
        Replacement = new (1, Stopwatch.GetElapsedTime(startReplacement, endReplacement)),
      }
    };
    
    if (Interceptor is null) return result;
    
    var interceptorStart = Stopwatch.GetTimestamp();
    var interceptedResult = Interceptor.Transform(result);
    var interceptorEnd = Stopwatch.GetTimestamp();
    if (interceptedResult == result) return result;
    
    var end = Stopwatch.GetTimestamp();
    
    return interceptedResult with {
      Duration = Stopwatch.GetElapsedTime(start, end),
      OperatorMetrics = interceptedResult.OperatorMetrics with {
        Interception = new(1, Stopwatch.GetElapsedTime(interceptorStart, interceptorEnd))
      }
    };
  }
  protected override EvolutionStrategyResult AggregateResult(EvolutionStrategyIterationResult iterationResult, EvolutionStrategyResult? algorithmResult) {
    return new EvolutionStrategyResult() {
      CurrentGeneration = iterationResult.Generation,
      TotalGenerations = iterationResult.Generation,
      CurrentDuration = iterationResult.Duration,
      TotalDuration = iterationResult.Duration + (algorithmResult?.TotalDuration ?? TimeSpan.Zero),
      CurrentMutationStrength = iterationResult.MutationStrength,
      CurrentOperatorMetrics = iterationResult.OperatorMetrics,
      TotalOperatorMetrics = iterationResult.OperatorMetrics + (algorithmResult?.TotalOperatorMetrics ?? new EvolutionStrategyOperatorMetrics()),
      Objective = iterationResult.Objective,
      CurrentPopulation = iterationResult.Population,
    };
  }
}
