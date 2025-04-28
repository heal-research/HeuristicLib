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
  
  public static EvolutionStrategyOperatorMetrics Aggregate(EvolutionStrategyOperatorMetrics left, EvolutionStrategyOperatorMetrics right) {
    return new EvolutionStrategyOperatorMetrics {
      Creation = left.Creation + right.Creation,
      Decoding = left.Decoding + right.Decoding,
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


public record EvolutionStrategyIterationResult : IContinuableIterationResult<EvolutionStrategyState> {
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
  
  public EvolutionStrategyState GetContinuationState() => new() {
    Generation = Generation /*+ 1*/,
    MutationStrength = MutationStrength,
    Population = Population.Select(i => new EvaluatedIndividual<RealVector>(i.Genotype, i.Fitness)).ToArray()
  };
  
  public EvolutionStrategyState GetRestartState() => GetContinuationState() with { Generation = 0 };
}

public record EvolutionStrategyResult : IAlgorithmResult {
  public required TimeSpan TotalDuration { get; init; }
  public required int TotalGenerations { get; init; }
  public required EvolutionStrategyOperatorMetrics OperatorMetrics { get; init; }
  public required EvaluatedIndividual<RealVector>? BestSolution { get; init; }
}

public record class EvolutionStrategy
  : IterativeAlgorithm<RealVector, RealVectorEncoding, EvolutionStrategyState, EvolutionStrategyIterationResult, EvolutionStrategyResult> {
  public int PopulationSize { get; }
  public int Children { get; }
  public EvolutionStrategyType Strategy { get; }

  public Creator<RealVector, RealVectorEncoding> Creator { get; }

  //public ICrossover<RealVector, RealVectorEncoding>? Crossover { get; }
  public Mutator<RealVector, RealVectorEncoding> Mutator { get; }
  public double InitialMutationStrength { get; }
  public Selector Selector { get; }
  public int RandomSeed { get; }
  public Interceptor<EvolutionStrategyIterationResult>? Interceptor { get; }

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
    Terminator<EvolutionStrategyIterationResult> terminator,
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
      TotalGenerations = totalGenerations,
      TotalDuration = totalDuration,
      OperatorMetrics = totalMetrics,
      BestSolution = bestSolution
    };
  }
  
  protected override EvolutionStrategyIterationResult ExecuteInitialization<TPhenotype>(IEncodedProblem<TPhenotype, RealVector, RealVectorEncoding> problem) {
    var start = Stopwatch.GetTimestamp();

    //var random = new SystemRandomNumberGenerator(Algorithm.RandomSeed);
    
    var startCreating = Stopwatch.GetTimestamp();
    var newPopulation = Enumerable.Range(0, Parameters.PopulationSize).Select(i => Creator.Create(problem.Encoding, Random)).ToArray();
    var endCreating = Stopwatch.GetTimestamp();
    
    var genotypePopulation = newPopulation;
    
    var startDecoding = Stopwatch.GetTimestamp();
    var phenotypePopulation = genotypePopulation.Select(genotype => problem.Decoder.Decode(genotype)).ToArray();
    var endDecoding = Stopwatch.GetTimestamp();
    
    var startEvaluating = Stopwatch.GetTimestamp();
    var fitnesses = phenotypePopulation.Select(phenotype => problem.Evaluator.Evaluate(phenotype)).ToArray();
    var endEvaluating = Stopwatch.GetTimestamp();

    var population = Population.From(genotypePopulation, /*phenotypePopulation,*/ fitnesses);

    var endBeforeInterceptor = Stopwatch.GetTimestamp();
    
    var result = new EvolutionStrategyIterationResult() {
      Generation = 0,
      MutationStrength = Parameters.InitialMutationStrength,
      Objective = problem.Objective,
      Population = population,
      TotalDuration = Stopwatch.GetElapsedTime(start, endBeforeInterceptor),
      OperatorMetrics = new () {
        Creation = new(genotypePopulation.Length, Stopwatch.GetElapsedTime(startCreating, endCreating)),
        Decoding = new(phenotypePopulation.Length, Stopwatch.GetElapsedTime(startDecoding, endDecoding)),
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
      TotalDuration = Stopwatch.GetElapsedTime(start, end),
      OperatorMetrics = interceptedResult.OperatorMetrics with {
        Interception = new(1, Stopwatch.GetElapsedTime(interceptorStart, interceptorEnd))
      } 
    };
  }
  
  protected override EvolutionStrategyIterationResult ExecuteIteration<TPhenotype>(IEncodedProblem<TPhenotype, RealVector, RealVectorEncoding> problem, EvolutionStrategyState state) {
    var start = Stopwatch.GetTimestamp();

    // int newRandomSeed = SeedSequence.GetSeed(Algorithm.RandomSeed, state.Generation);
    // var random = new SystemRandomNumberGenerator(newRandomSeed);
    
    var oldPopulation = state.Population;
    
    var startSelection = Stopwatch.GetTimestamp();
    var randomSelector = new RandomSelector().CreateInstance(); // improve
    var parents = randomSelector.Select(oldPopulation, problem.Objective, Parameters.PopulationSize, Random).ToList();
    var endSelection = Stopwatch.GetTimestamp();
     
    var genotypePopulation = new RealVector[parents.Count];
    var startMutation = Stopwatch.GetTimestamp();
    for (int i = 0; i < parents.Count; i += 2) {
      var parent = parents[i];
      var child = Mutator.Mutate(parent.Genotype, problem.Encoding, Random);
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
    
    var population = Population.From(genotypePopulation, /*phenotypePopulation,*/ fitnesses);
    
    var startReplacement = Stopwatch.GetTimestamp();
    Replacer replacer = Parameters.Strategy switch {
      EvolutionStrategyType.Comma => new ElitismReplacer(0),
      EvolutionStrategyType.Plus => new PlusSelectionReplacer(),
      _ => throw new InvalidOperationException($"Unknown strategy {Parameters.Strategy}")
    };
    var newPopulation = replacer.CreateInstance().Replace(oldPopulation, population, problem.Objective, Random);
    var endReplacement = Stopwatch.GetTimestamp();
    
    var endBeforeInterceptor = Stopwatch.GetTimestamp();

    var result = new EvolutionStrategyIterationResult() {
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
    
    if (Interceptor is null) return result;
    
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
}
