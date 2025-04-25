using System.Diagnostics;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Algorithms.NSGA2;

public class NSGA2<TGenotype, TEncoding> 
  : IterativeAlgorithm<TGenotype, TEncoding, NSGA2State<TGenotype>, NSGA2IterationResult<TGenotype>, NSGA2Result<TGenotype>> 
  where TEncoding : IEncoding<TGenotype>
{
  public int PopulationSize { get; }
  public ICreator<TGenotype, TEncoding> Creator { get; }
  public ICrossover<TGenotype, TEncoding> Crossover { get; }
  public IMutator<TGenotype, TEncoding> Mutator { get; }
  public double MutationRate { get; }
  //public ISelector Selector { get; }
  public IReplacer Replacer { get; }
  public int RandomSeed { get; }
  public IInterceptor<NSGA2IterationResult<TGenotype>>? Interceptor { get; }
  
  public NSGA2(
    int populationSize,
    ICreator<TGenotype, TEncoding> creator,
    ICrossover<TGenotype, TEncoding> crossover,
    IMutator<TGenotype, TEncoding> mutator, double mutationRate,
    /*ISelector selector,*/ IReplacer replacer,
    int randomSeed,
    ITerminator<NSGA2IterationResult<TGenotype>> terminator,
    IInterceptor<NSGA2IterationResult<TGenotype>>? interceptor = null
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
  
  public override NSGA2Result<TGenotype> Execute<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, NSGA2State<TGenotype>? initialState = null) {
    IReadOnlyList<EvaluatedIndividual<TGenotype>> paretoFront = [];
    
    int totalGenerations = 0;
    TimeSpan totalDuration = TimeSpan.Zero;
    var totalMetrics = new NSGA2OperatorMetrics();
    
    foreach (var result in ExecuteStreaming(problem, initialState)) {
      paretoFront = Population.ExtractParetoFront(paretoFront.Concat(result.ParetoFront).ToList(), problem.Objective);
      totalGenerations += 1;
      totalDuration += result.TotalDuration;
      totalMetrics += result.OperatorMetrics;
    }
    
    return new NSGA2Result<TGenotype> {
      UsedRandomSeed = RandomSeed,
      TotalGenerations = totalGenerations,
      TotalDuration = totalDuration,
      OperatorMetrics = totalMetrics,
      ParetoFront = paretoFront
    };
  }
  
  protected override NSGA2IterationResult<TGenotype> ExecuteInitialization<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem) {
    var start = Stopwatch.GetTimestamp();

    var random = new SystemRandomNumberGenerator(RandomSeed);

    var startCreating = Stopwatch.GetTimestamp();
    var newPopulation = Enumerable.Range(0, PopulationSize).Select(i => Creator.Create(problem.Encoding, random)).ToArray();
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
    
    var result = new NSGA2IterationResult<TGenotype>() {
      UsedGenerationRandomSeed = RandomSeed,
      Generation = 0,
      Objective = problem.Objective,
      Population = population,
      TotalDuration = Stopwatch.GetElapsedTime(start, endBeforeInterceptor),
      OperatorMetrics = new NSGA2OperatorMetrics() {
        Creation = new OperatorMetric(genotypePopulation.Length, Stopwatch.GetElapsedTime(startCreating, endCreating)),
        Decoding = new OperatorMetric(phenotypePopulation.Length, Stopwatch.GetElapsedTime(startDecoding, endDecoding)),
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
      TotalDuration = Stopwatch.GetElapsedTime(start, end),
      OperatorMetrics = interceptedResult.OperatorMetrics with {
        Interception = new OperatorMetric(1, Stopwatch.GetElapsedTime(interceptorStart, interceptorEnd))
      }
    };
  }

  protected override NSGA2IterationResult<TGenotype> ExecuteIteration<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, NSGA2State<TGenotype> state) {
    var start = Stopwatch.GetTimestamp();
    
    int newRandomSeed = SeedSequence.GetSeed(RandomSeed, state.Generation);
    var random = new SystemRandomNumberGenerator(newRandomSeed);
    
    int offspringCount = Replacer.GetOffspringCount(PopulationSize);

    var oldPopulation = state.Population;
    
    
    var startSelection = Stopwatch.GetTimestamp();
    var Selector = new RandomSelector(); // ToDo: implement NSGA-specific selection (pareto-based selection)
    var parents = Selector.Select(oldPopulation, problem.Objective, offspringCount * 2, random).ToList();
    var endSelection = Stopwatch.GetTimestamp();
     
    var genotypePopulation = new TGenotype[offspringCount];
    var startCrossover = Stopwatch.GetTimestamp();
    int crossoverCount = 0;
    for (int i = 0; i < parents.Count; i += 2) {
      var parent1 = parents[i];
      var parent2 = parents[i + 1];
      var child = Crossover.Cross(parent1.Genotype, parent2.Genotype, problem.Encoding, random);
      genotypePopulation[i / 2] = child;
      crossoverCount++;
    }
    var endCrossover = Stopwatch.GetTimestamp();
    
    var startMutation = Stopwatch.GetTimestamp();
    int mutationCount = 0;
    for (int i = 0; i < genotypePopulation.Length; i++) {
      if (random.Random() < MutationRate) {
        genotypePopulation[i] = Mutator.Mutate(genotypePopulation[i], problem.Encoding, random);
        mutationCount++;
      }
    }
    var endMutation = Stopwatch.GetTimestamp();
    
    var startDecoding = Stopwatch.GetTimestamp();
    var phenotypePopulation = genotypePopulation.Select(genotype => problem.Decoder.Decode(genotype)).ToArray();
    var endDecoding = Stopwatch.GetTimestamp();
    
    var startEvaluation = Stopwatch.GetTimestamp();
    var fitnesses = phenotypePopulation.Select(phenotype => problem.Evaluator.Evaluate(phenotype)).ToArray();
    var endEvaluation = Stopwatch.GetTimestamp();
    
    var population = Population.From(genotypePopulation, /*phenotypePopulation,*/ fitnesses);
    
    var startReplacement = Stopwatch.GetTimestamp();
    var newPopulation = Replacer.Replace(oldPopulation, population, problem.Objective, random);
    var endReplacement = Stopwatch.GetTimestamp();
    
    var endBeforeInterceptor = Stopwatch.GetTimestamp();
    
    var result = new NSGA2IterationResult<TGenotype>() {
      UsedGenerationRandomSeed = newRandomSeed,
      Generation = state.Generation + 1,
      Objective = problem.Objective,
      Population = newPopulation,
      TotalDuration = Stopwatch.GetElapsedTime(start, endBeforeInterceptor),
      OperatorMetrics = new NSGA2OperatorMetrics() {
        Selection = new OperatorMetric(parents.Count, Stopwatch.GetElapsedTime(startSelection, endSelection)),
        Crossover = new OperatorMetric(crossoverCount, Stopwatch.GetElapsedTime(startCrossover, endCrossover)),
        Mutation = new OperatorMetric(mutationCount, Stopwatch.GetElapsedTime(startMutation, endMutation)),
        Decoding = new OperatorMetric(phenotypePopulation.Length, Stopwatch.GetElapsedTime(startDecoding, endDecoding)),
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
      TotalDuration = Stopwatch.GetElapsedTime(start, end),
      OperatorMetrics = interceptedResult.OperatorMetrics with {
        Interception = new OperatorMetric(1, Stopwatch.GetElapsedTime(interceptorStart, interceptorEnd))
      }
    };
  }
}


public record NSGA2State<TGenotype> {
  public required int UsedRandomSeed { get; init; }
  public required int Generation { get; init; }
  public required IReadOnlyList<EvaluatedIndividual<TGenotype>> Population { get; init; }
}

public record NSGA2OperatorMetrics {
  public OperatorMetric Creation { get; init; } = OperatorMetric.Zero;
  public OperatorMetric Decoding { get; init; } = OperatorMetric.Zero;
  public OperatorMetric Evaluation { get; init; } = OperatorMetric.Zero;
  public OperatorMetric Selection { get; init; } = OperatorMetric.Zero;
  public OperatorMetric Crossover { get; init; } = OperatorMetric.Zero;
  public OperatorMetric Mutation { get; init; } = OperatorMetric.Zero;
  public OperatorMetric Replacement { get; init; } = OperatorMetric.Zero;
  public OperatorMetric Interception { get; init; } = OperatorMetric.Zero;
  
  public static NSGA2OperatorMetrics Aggregate(NSGA2OperatorMetrics left, NSGA2OperatorMetrics right) {
    return new NSGA2OperatorMetrics {
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
  public static NSGA2OperatorMetrics operator +(NSGA2OperatorMetrics left, NSGA2OperatorMetrics right) => Aggregate(left, right);
}

public record NSGA2IterationResult<TGenotype> : IMultiObjectiveIterationResult<TGenotype>, IContinuableIterationResult<NSGA2State<TGenotype>> {
  int IIterationResult.UsedIterationRandomSeed => UsedGenerationRandomSeed;
  public required int UsedGenerationRandomSeed { get; init; }
  
  int IIterationResult.Iteration => Generation;
  public required int Generation { get; init; }
  
  public required Objective Objective { get; init; }
  
  public required TimeSpan TotalDuration { get; init; }
  public required NSGA2OperatorMetrics OperatorMetrics { get; init; }

  public required IReadOnlyList<EvaluatedIndividual<TGenotype>> Population { get; init; }
  
  public NSGA2IterationResult() {
    paretoFront = new Lazy<IReadOnlyList<EvaluatedIndividual<TGenotype>>>(() => {
      return global::HEAL.HeuristicLib.Population.ExtractParetoFront(Population, Objective);
    });
  }
  
  private readonly Lazy<IReadOnlyList<EvaluatedIndividual<TGenotype>>> paretoFront;
  public IReadOnlyList<EvaluatedIndividual<TGenotype>> ParetoFront => paretoFront.Value;
  
  public NSGA2State<TGenotype> GetState() => new() {
    UsedRandomSeed = UsedGenerationRandomSeed,
    Generation = Generation,
    Population = Population
  };
}

public record NSGA2Result<TGenotype> : IMultiObjectiveAlgorithmResult<TGenotype> {
  [Obsolete("Not necessary on result, since it can be obtained form the algorithm (parameters)")]
  public required int UsedRandomSeed { get; init; }
  public required TimeSpan TotalDuration { get; init; }
  public required int TotalGenerations { get; init; }
  public required NSGA2OperatorMetrics OperatorMetrics { get; init; }
  public required IReadOnlyList<EvaluatedIndividual<TGenotype>> ParetoFront { get; init; }
}
