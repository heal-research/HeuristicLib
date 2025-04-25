using System.Diagnostics;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;

public class GeneticAlgorithm<TGenotype, TEncoding>
  : IterativeAlgorithm<TGenotype, TEncoding, GeneticAlgorithmState<TGenotype>, GeneticAlgorithmIterationResult<TGenotype>, GeneticAlgorithmResult<TGenotype>>
  where TEncoding : IEncoding<TGenotype>
{
  public int PopulationSize { get; }
  public ICreator<TGenotype, TEncoding> Creator { get; }
  public ICrossover<TGenotype, TEncoding> Crossover { get; }
  public IMutator<TGenotype, TEncoding> Mutator { get; }
  public double MutationRate { get; }
  public ISelector Selector { get; }
  public IReplacer Replacer { get; }
  public int RandomSeed { get; }
  public IInterceptor<GeneticAlgorithmIterationResult<TGenotype>>? Interceptor { get; }
  
  public GeneticAlgorithm(
    int populationSize,
    ICreator<TGenotype, TEncoding> creator,
    ICrossover<TGenotype, TEncoding> crossover,
    IMutator<TGenotype, TEncoding> mutator, double mutationRate,
    ISelector selector, IReplacer replacer,
    int randomSeed,
    ITerminator<GeneticAlgorithmIterationResult<TGenotype>> terminator,
    IInterceptor<GeneticAlgorithmIterationResult<TGenotype>>? interceptor = null
  ) : base(terminator) {
    PopulationSize = populationSize;
    Creator = creator;
    Crossover = crossover;
    Mutator = mutator;
    MutationRate = mutationRate;
    Selector = selector;
    Replacer = replacer;
    RandomSeed = randomSeed;
    Interceptor = interceptor;
  }
  
  public override GeneticAlgorithmResult<TGenotype> Execute<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, GeneticAlgorithmState<TGenotype>? initialState = null) {
    EvaluatedIndividual<TGenotype>? bestSolution = null;
    var comparer = problem.Objective.TotalOrderComparer;
    
    int totalGenerations = 0;
    TimeSpan totalDuration = TimeSpan.Zero;
    var totalMetrics = new GeneticAlgorithmOperatorMetrics();
    
    foreach (var result in ExecuteStreaming(problem, initialState)) {
      if (bestSolution is null || comparer.Compare(result.BestSolution.Fitness, bestSolution.Fitness) < 0) // ToDo: better "IsBetter" method.
        bestSolution = result.BestSolution;
      totalGenerations += 1;
      totalDuration += result.TotalDuration;
      totalMetrics += result.OperatorMetrics;
    }
    
    return new GeneticAlgorithmResult<TGenotype> {
      UsedRandomSeed = RandomSeed,
      TotalGenerations = totalGenerations,
      TotalDuration = totalDuration,
      OperatorMetrics = totalMetrics,
      BestSolution = bestSolution
    };
  }
  
  protected override GeneticAlgorithmIterationResult<TGenotype> ExecuteInitialization<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem) {
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
    
    var result = new GeneticAlgorithmIterationResult<TGenotype>() {
      UsedGenerationRandomSeed = RandomSeed,
      Generation = 0,
      Objective = problem.Objective,
      Population = population,
      TotalDuration = Stopwatch.GetElapsedTime(start, endBeforeInterceptor),
      OperatorMetrics = new GeneticAlgorithmOperatorMetrics {
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
  
  protected override GeneticAlgorithmIterationResult<TGenotype> ExecuteIteration<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, GeneticAlgorithmState<TGenotype> state) {
    var start = Stopwatch.GetTimestamp();
    
    int newRandomSeed = SeedSequence.GetSeed(RandomSeed, state.Generation);
    var random = new SystemRandomNumberGenerator(newRandomSeed);
    
    int offspringCount = Replacer.GetOffspringCount(PopulationSize);

    var oldPopulation = state.Population;
    
    var startSelection = Stopwatch.GetTimestamp();
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
    
    var result = new GeneticAlgorithmIterationResult<TGenotype>() {
      UsedGenerationRandomSeed = newRandomSeed,
      Generation = state.Generation + 1,
      Objective = problem.Objective,
      Population = newPopulation,
      TotalDuration = Stopwatch.GetElapsedTime(start, endBeforeInterceptor),
      OperatorMetrics = new GeneticAlgorithmOperatorMetrics() {
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

public static class GeneticAlgorithm {
  public static GeneticAlgorithm<TGenotype, TEncoding> FromConfiguration<TGenotype, TEncoding>(
    GeneticAlgorithmConfiguration<TGenotype, TEncoding> configuration)
    where TEncoding : IEncoding<TGenotype> 
  {
    return configuration.Build();
  }
}
