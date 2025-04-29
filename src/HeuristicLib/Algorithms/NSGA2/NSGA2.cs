using System.Diagnostics;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Algorithms.NSGA2;

public record class NSGA2<TGenotype, TSearchSpace>
  : IterativeAlgorithm<TGenotype, TSearchSpace, NSGA2State<TGenotype>, NSGA2IterationResult<TGenotype>, NSGA2Result<TGenotype>>
  where TSearchSpace : ISearchSpace<TGenotype> 
{
  public int PopulationSize { get; init; }
  public Creator<TGenotype, TSearchSpace> Creator { get; init; }
  public Crossover<TGenotype, TSearchSpace> Crossover { get; init; }
  public Mutator<TGenotype, TSearchSpace> Mutator { get; init; }

  public double MutationRate { get; init; }

  //public ISelector Selector { get; init; }
  public Replacer Replacer { get; init; }
  public int RandomSeed { get; init; }
  public Interceptor<NSGA2IterationResult<TGenotype>>? Interceptor { get; init; }

  public NSGA2(
    int populationSize,
    Creator<TGenotype, TSearchSpace> creator,
    Crossover<TGenotype, TSearchSpace> crossover,
    Mutator<TGenotype, TSearchSpace> mutator, double mutationRate,
    /*ISelector selector,*/ Replacer replacer,
    int randomSeed,
    Terminator<NSGA2Result<TGenotype>> terminator,
    Interceptor<NSGA2IterationResult<TGenotype>>? interceptor = null
  ) : base(terminator) {
    PopulationSize = populationSize;
    Creator = creator;
    Crossover = crossover;
    Mutator = mutator;
    MutationRate = mutationRate;
    //Selector = selector;
    Replacer = replacer;
    RandomSeed = randomSeed;
    Interceptor = interceptor;
  }

  public override NSGA2Instance<TGenotype, TSearchSpace> CreateInstance() {
    return new NSGA2Instance<TGenotype, TSearchSpace>(this);
  }
}

public class NSGA2Instance<TGenotype, TSearchSpace>
  : IterativeAlgorithmInstance<TGenotype, TSearchSpace, NSGA2State<TGenotype>, NSGA2IterationResult<TGenotype>, NSGA2Result<TGenotype>, NSGA2<TGenotype, TSearchSpace>>
  where TSearchSpace : ISearchSpace<TGenotype> 
{
  public IRandomNumberGenerator Random { get; }
  public ICreatorInstance<TGenotype, TSearchSpace> Creator { get; }
  public ICrossoverInstance<TGenotype, TSearchSpace> Crossover { get; }
  public IMutatorInstance<TGenotype, TSearchSpace> Mutator { get; }
  public IReplacerInstance Replacer { get; }
  public IInterceptorInstance<NSGA2IterationResult<TGenotype>>? Interceptor { get; }
  
  public NSGA2Instance(NSGA2<TGenotype, TSearchSpace> parameters) : base (parameters) {
    Random = new SystemRandomNumberGenerator(parameters.RandomSeed);
    Creator = parameters.Creator.CreateInstance();
    Crossover = parameters.Crossover.CreateInstance();
    Mutator = parameters.Mutator.CreateInstance();
    Replacer = parameters.Replacer.CreateInstance();
    Interceptor = parameters.Interceptor?.CreateInstance();
  }

  // public override NSGA2Result<TGenotype> Execute<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TSearchSpace> problem, NSGA2State<TGenotype>? initialState = null) {
  //   IReadOnlyList<EvaluatedIndividual<TGenotype>> paretoFront = [];
  //   
  //   int totalGenerations = 0;
  //   TimeSpan totalDuration = TimeSpan.Zero;
  //   var totalMetrics = new NSGA2OperatorMetrics();
  //   
  //   foreach (var result in ExecuteStreaming(problem, initialState)) {
  //     paretoFront = Population.ExtractParetoFront(paretoFront.Concat(result.ParetoFront).ToList(), problem.Objective);
  //     totalGenerations += 1;
  //     totalDuration += result.TotalDuration;
  //     totalMetrics += result.OperatorMetrics;
  //   }
  //   
  //   return new NSGA2Result<TGenotype> {
  //     TotalGenerations = totalGenerations,
  //     TotalDuration = totalDuration,
  //     OperatorMetrics = totalMetrics,
  //     CurrentParetoFront = paretoFront
  //   };
  // }
  
  protected override NSGA2IterationResult<TGenotype> ExecuteInitialization(IOptimizable<TGenotype, TSearchSpace> optimizable) {
    var start = Stopwatch.GetTimestamp();

    // var random = new SystemRandomNumberGenerator(Parameters.RandomSeed);

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
    
    var result = new NSGA2IterationResult<TGenotype>() {
      Generation = 0,
      Objective = optimizable.Objective,
      Population = population,
      Duration = Stopwatch.GetElapsedTime(start, endBeforeInterceptor),
      OperatorMetrics = new NSGA2OperatorMetrics() {
        Creation = new OperatorMetric(genotypes.Length, Stopwatch.GetElapsedTime(startCreating, endCreating)),
        // Decoding = new OperatorMetric(phenotypePopulation.Length, Stopwatch.GetElapsedTime(startDecoding, endDecoding)),
        Evaluation = new OperatorMetric(fitnesses.Length, Stopwatch.GetElapsedTime(startEvaluating, endEvaluating)),
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
        Interception = new OperatorMetric(1, Stopwatch.GetElapsedTime(interceptorStart, interceptorEnd))
      }
    };
  }

  protected override NSGA2IterationResult<TGenotype> ExecuteIteration(IOptimizable<TGenotype, TSearchSpace> optimizable, NSGA2State<TGenotype> state) {
    var start = Stopwatch.GetTimestamp();
    
    // int newRandomSeed = SeedSequence.GetSeed(Algorithm.RandomSeed, state.Generation);
    // var random = new SystemRandomNumberGenerator(newRandomSeed);
    
    int offspringCount = Replacer.GetOffspringCount(Parameters.PopulationSize);

    var oldPopulation = state.Population;
    
    var startSelection = Stopwatch.GetTimestamp();
    var selector = new RandomSelector().CreateInstance(); // ToDo: implement NSGA-specific selection (pareto-based selection)
    var parents = selector.Select(oldPopulation, optimizable.Objective, offspringCount * 2, Random).ToList();
    var endSelection = Stopwatch.GetTimestamp();
     
    var genotypes = new TGenotype[offspringCount];
    var startCrossover = Stopwatch.GetTimestamp();
    int crossoverCount = 0;
    for (int i = 0; i < parents.Count; i += 2) {
      var parent1 = parents[i];
      var parent2 = parents[i + 1];
      var child = Crossover.Cross(parent1.Genotype, parent2.Genotype, optimizable.SearchSpace, Random);
      genotypes[i / 2] = child;
      crossoverCount++;
    }
    var endCrossover = Stopwatch.GetTimestamp();
    
    var startMutation = Stopwatch.GetTimestamp();
    int mutationCount = 0;
    for (int i = 0; i < genotypes.Length; i++) {
      if (Random.Random() < Parameters.MutationRate) {
        genotypes[i] = Mutator.Mutate(genotypes[i], optimizable.SearchSpace, Random);
        mutationCount++;
      }
    }
    var endMutation = Stopwatch.GetTimestamp();
    
    // var startDecoding = Stopwatch.GetTimestamp();
    // var phenotypePopulation = genotypes.Select(genotype => optimizable.Decoder.Decode(genotype)).ToArray();
    // var endDecoding = Stopwatch.GetTimestamp();
    
    var startEvaluation = Stopwatch.GetTimestamp();
    var fitnesses = genotypes.Select(genotype => optimizable.Evaluate(genotype)).ToArray();
    var endEvaluation = Stopwatch.GetTimestamp();
    
    var population = Population.From(genotypes, /*phenotypePopulation,*/ fitnesses);
    
    var startReplacement = Stopwatch.GetTimestamp();
    var newPopulation = Replacer.Replace(oldPopulation, population, optimizable.Objective, Random);
    var endReplacement = Stopwatch.GetTimestamp();
    
    var endBeforeInterceptor = Stopwatch.GetTimestamp();
    
    var result = new NSGA2IterationResult<TGenotype>() {
      Generation = state.Generation + 1,
      Objective = optimizable.Objective,
      Population = newPopulation,
      Duration = Stopwatch.GetElapsedTime(start, endBeforeInterceptor),
      OperatorMetrics = new NSGA2OperatorMetrics() {
        Selection = new OperatorMetric(parents.Count, Stopwatch.GetElapsedTime(startSelection, endSelection)),
        Crossover = new OperatorMetric(crossoverCount, Stopwatch.GetElapsedTime(startCrossover, endCrossover)),
        Mutation = new OperatorMetric(mutationCount, Stopwatch.GetElapsedTime(startMutation, endMutation)),
        // Decoding = new OperatorMetric(phenotypePopulation.Length, Stopwatch.GetElapsedTime(startDecoding, endDecoding)),
        Evaluation = new OperatorMetric(fitnesses.Length, Stopwatch.GetElapsedTime(startEvaluation, endEvaluation)),
        Replacement = new OperatorMetric(1, Stopwatch.GetElapsedTime(startReplacement, endReplacement))
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
        Interception = new OperatorMetric(1, Stopwatch.GetElapsedTime(interceptorStart, interceptorEnd))
      }
    };
  }
  protected override NSGA2Result<TGenotype> AggregateResult(NSGA2IterationResult<TGenotype> iterationResult, NSGA2Result<TGenotype>? algorithmResult) {
    return new NSGA2Result<TGenotype>() {
      CurrentGeneration = iterationResult.Generation,
      TotalGenerations = iterationResult.Generation,
      CurrentDuration = iterationResult.Duration,
      TotalDuration = iterationResult.Duration + (algorithmResult?.TotalDuration ?? TimeSpan.Zero),
      CurrentOperatorMetrics = iterationResult.OperatorMetrics,
      TotalOperatorMetrics = iterationResult.OperatorMetrics + (algorithmResult?.TotalOperatorMetrics ?? new NSGA2OperatorMetrics()),
      Objective = iterationResult.Objective,
      CurrentPopulation = iterationResult.Population,
    };
  }
}


public record NSGA2State<TGenotype> {
  public required int Generation { get; init; }
  public required IReadOnlyList<EvaluatedIndividual<TGenotype>> Population { get; init; }
}

public record NSGA2OperatorMetrics {
  public OperatorMetric Creation { get; init; } = OperatorMetric.Zero;
  // public OperatorMetric Decoding { get; init; } = OperatorMetric.Zero;
  public OperatorMetric Evaluation { get; init; } = OperatorMetric.Zero;
  public OperatorMetric Selection { get; init; } = OperatorMetric.Zero;
  public OperatorMetric Crossover { get; init; } = OperatorMetric.Zero;
  public OperatorMetric Mutation { get; init; } = OperatorMetric.Zero;
  public OperatorMetric Replacement { get; init; } = OperatorMetric.Zero;
  public OperatorMetric Interception { get; init; } = OperatorMetric.Zero;
  
  public static NSGA2OperatorMetrics Aggregate(NSGA2OperatorMetrics left, NSGA2OperatorMetrics right) {
    return new NSGA2OperatorMetrics {
      Creation = left.Creation + right.Creation,
      // Decoding = left.Decoding + right.Decoding,
      Evaluation = left.Evaluation + right.Evaluation,
      Selection = left.Selection + right.Selection,
      Crossover = left.Crossover + right.Crossover,
      Mutation = left.Mutation + right.Mutation,
      Replacement = left.Replacement + right.Replacement,
      Interception = left.Interception + right.Interception
    };
  }
  public static NSGA2OperatorMetrics operator +(NSGA2OperatorMetrics left, NSGA2OperatorMetrics right) => Aggregate(left, right);
}

public record NSGA2IterationResult<TGenotype>  {
  public required int Generation { get; init; }
  public required TimeSpan Duration { get; init; }
  public required NSGA2OperatorMetrics OperatorMetrics { get; init; }
  public required Objective Objective { get; init; }
  public required IReadOnlyList<EvaluatedIndividual<TGenotype>> Population { get; init; }
}

public record NSGA2Result<TGenotype> : IMultiObjectiveAlgorithmResult<TGenotype>, IContinuableAlgorithmResult<NSGA2State<TGenotype>> {
  int IAlgorithmResult.CurrentIteration => CurrentGeneration;
  int IAlgorithmResult.TotalIterations => TotalGenerations;
  
  public required int CurrentGeneration { get; init; }
  public required int TotalGenerations { get; init; }
  
  public required TimeSpan CurrentDuration { get; init; }
  public required TimeSpan TotalDuration { get; init; }
  
  public required NSGA2OperatorMetrics CurrentOperatorMetrics { get; init; }
  public required NSGA2OperatorMetrics TotalOperatorMetrics { get; init; }

  public required Objective Objective { get; init; }
  
  public required IReadOnlyList<EvaluatedIndividual<TGenotype>> CurrentPopulation { get; init; }
  
  public NSGA2Result() {
    currentParetoFront = new Lazy<IReadOnlyList<EvaluatedIndividual<TGenotype>>>(() => {
      return Population.ExtractParetoFront(CurrentPopulation, Objective);
    });
  }
  
  private readonly Lazy<IReadOnlyList<EvaluatedIndividual<TGenotype>>> currentParetoFront;
  public IReadOnlyList<EvaluatedIndividual<TGenotype>> CurrentParetoFront => currentParetoFront.Value;
  
  public NSGA2State<TGenotype> GetContinuationState() => new() {
    Generation = CurrentGeneration,
    Population = CurrentPopulation
  };

  public NSGA2State<TGenotype> GetRestartState() => GetContinuationState() with { Generation = 0 };
}
