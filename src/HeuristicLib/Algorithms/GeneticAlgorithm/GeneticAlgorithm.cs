using System.Diagnostics;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;

public record class GeneticAlgorithm<TGenotype, TEncoding>
  : IterativeAlgorithm<TGenotype, TEncoding, GeneticAlgorithmState<TGenotype>, GeneticAlgorithmIterationResult<TGenotype>, GeneticAlgorithmResult<TGenotype>>
  where TEncoding : IEncoding<TGenotype> 
{
  public int PopulationSize { get; init; }
  public Creator<TGenotype, TEncoding> Creator { get; init; }
  public Crossover<TGenotype, TEncoding> Crossover { get; init; }
  public Mutator<TGenotype, TEncoding> Mutator { get; init; }
  public double MutationRate { get; init; }
  public Selector Selector { get; init; }
  public Replacer Replacer { get; init; }
  public int RandomSeed { get; init; }
  // ToDo: Interceptor could also be defined on AlgorithmResult and handled at the IterativeAlgorithm level.
  public Interceptor<GeneticAlgorithmIterationResult<TGenotype>>? Interceptor { get; init; }

  public GeneticAlgorithm(
    int populationSize,
    Creator<TGenotype, TEncoding> creator,
    Crossover<TGenotype, TEncoding> crossover,
    Mutator<TGenotype, TEncoding> mutator, double mutationRate,
    Selector selector, Replacer replacer,
    int randomSeed,
    Terminator<GeneticAlgorithmResult<TGenotype>> terminator,
    Interceptor<GeneticAlgorithmIterationResult<TGenotype>>? interceptor = null
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

  public override GeneticAlgorithmInstance<TGenotype, TEncoding> CreateInstance() {
    return new GeneticAlgorithmInstance<TGenotype, TEncoding>(this);
  }
}

// public interface IGeneticAlgorithmInstance<TGenotype, TEncoding> {
//   
// }

public class GeneticAlgorithmInstance<TGenotype, TEncoding>
  : IterativeAlgorithmInstance<TGenotype, TEncoding, GeneticAlgorithmState<TGenotype>, GeneticAlgorithmIterationResult<TGenotype>, GeneticAlgorithmResult<TGenotype>, GeneticAlgorithm<TGenotype, TEncoding>>
  where TEncoding : IEncoding<TGenotype> 
{
  public IRandomNumberGenerator Random { get; }
  public ICreatorInstance<TGenotype, TEncoding> Creator { get; }
  public ICrossoverInstance<TGenotype, TEncoding> Crossover { get; }
  public IMutatorInstance<TGenotype, TEncoding> Mutator { get; }
  public ISelectorInstance Selector { get; }
  public IReplacerInstance Replacer { get; }
  public IInterceptorInstance<GeneticAlgorithmIterationResult<TGenotype>>? Interceptor { get; }
  
  public GeneticAlgorithmInstance(GeneticAlgorithm<TGenotype, TEncoding> parameters) : base(parameters) {
    Random = new SystemRandomNumberGenerator(parameters.RandomSeed);
    Creator = parameters.Creator.CreateInstance();
    Crossover = parameters.Crossover.CreateInstance();
    Mutator = parameters.Mutator.CreateInstance();
    Selector = parameters.Selector.CreateInstance();
    Replacer = parameters.Replacer.CreateInstance();
    Interceptor = parameters.Interceptor?.CreateInstance();
  }

  // public override GeneticAlgorithmResult<TGenotype> Execute<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, GeneticAlgorithmState<TGenotype>? initialState = null) {
  //   EvaluatedIndividual<TGenotype>? bestSolution = null;
  //   var comparer = problem.Objective.TotalOrderComparer;
  //   
  //   foreach (var result in ExecuteStreaming(problem, initialState)) {
  //     if (bestSolution is null || comparer.Compare(result.CurrentBestSolution.Fitness, bestSolution.Fitness) < 0) // ToDo: better "IsBetter" method.
  //       bestSolution = result.CurrentBestSolution;
  //     totalGenerations += 1;
  //     totalDuration += result.TotalDuration;
  //     totalMetrics += result.OperatorMetrics;
  //   }
  //   
  //   return new GeneticAlgorithmResult<TGenotype> {
  //     TotalGenerations = totalGenerations,
  //     TotalDuration = totalDuration,
  //     OperatorMetrics = totalMetrics,
  //     CurrentBestSolution = bestSolution
  //   };
  // }
  
  protected override GeneticAlgorithmIterationResult<TGenotype> ExecuteInitialization<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem) {
    var start = Stopwatch.GetTimestamp();

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
    
    var result = new GeneticAlgorithmIterationResult<TGenotype>() {
      Generation = 0,
      Duration = Stopwatch.GetElapsedTime(start, endBeforeInterceptor),
      OperatorMetrics = new GeneticAlgorithmOperatorMetrics {
        Creation = new OperatorMetric(genotypePopulation.Length, Stopwatch.GetElapsedTime(startCreating, endCreating)),
        Decoding = new OperatorMetric(phenotypePopulation.Length, Stopwatch.GetElapsedTime(startDecoding, endDecoding)),
        Evaluation = new OperatorMetric(fitnesses.Length, Stopwatch.GetElapsedTime(startEvaluating, endEvaluating)),
      },
      Population = population,
      Objective = problem.Objective
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
  
  protected override GeneticAlgorithmIterationResult<TGenotype> ExecuteIteration<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, GeneticAlgorithmState<TGenotype> state) {
    var start = Stopwatch.GetTimestamp();
    
    // int newRandomSeed = SeedSequence.GetSeed(Algorithm.RandomSeed, state.Generation);
    // var random = new SystemRandomNumberGenerator(newRandomSeed);
    
    int offspringCount = Replacer.GetOffspringCount(Parameters.PopulationSize);

    var oldPopulation = state.Population;
    
    var startSelection = Stopwatch.GetTimestamp();
    var parents = Selector.Select(oldPopulation, problem.Objective, offspringCount * 2, Random).ToList();
    var endSelection = Stopwatch.GetTimestamp();
     
    var genotypePopulation = new TGenotype[offspringCount];
    var startCrossover = Stopwatch.GetTimestamp();
    int crossoverCount = 0;
    for (int i = 0; i < parents.Count; i += 2) {
      var parent1 = parents[i];
      var parent2 = parents[i + 1];
      var child = Crossover.Cross(parent1.Genotype, parent2.Genotype, problem.Encoding, Random);
      genotypePopulation[i / 2] = child;
      crossoverCount++;
    }
    var endCrossover = Stopwatch.GetTimestamp();
    
    var startMutation = Stopwatch.GetTimestamp();
    int mutationCount = 0;
    for (int i = 0; i < genotypePopulation.Length; i++) {
      if (Random.Random() < Parameters.MutationRate) {
        genotypePopulation[i] = Mutator.Mutate(genotypePopulation[i], problem.Encoding, Random);
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
    var newPopulation = Replacer.Replace(oldPopulation, population, problem.Objective, Random);
    var endReplacement = Stopwatch.GetTimestamp();
    
    var endBeforeInterceptor = Stopwatch.GetTimestamp();
    
    var result = new GeneticAlgorithmIterationResult<TGenotype>() {
      Generation = state.Generation + 1,
      Duration = Stopwatch.GetElapsedTime(start, endBeforeInterceptor),
      OperatorMetrics = new GeneticAlgorithmOperatorMetrics() {
        Selection = new OperatorMetric(parents.Count, Stopwatch.GetElapsedTime(startSelection, endSelection)),
        Crossover = new OperatorMetric(crossoverCount, Stopwatch.GetElapsedTime(startCrossover, endCrossover)),
        Mutation = new OperatorMetric(mutationCount, Stopwatch.GetElapsedTime(startMutation, endMutation)),
        Decoding = new OperatorMetric(phenotypePopulation.Length, Stopwatch.GetElapsedTime(startDecoding, endDecoding)),
        Evaluation = new OperatorMetric(fitnesses.Length, Stopwatch.GetElapsedTime(startEvaluation, endEvaluation)),
        Replacement = new OperatorMetric(1, Stopwatch.GetElapsedTime(startReplacement, endReplacement))
      },
      Population = newPopulation,
      Objective = problem.Objective
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

  protected override GeneticAlgorithmResult<TGenotype> AggregateResult(GeneticAlgorithmIterationResult<TGenotype> iterationResult, GeneticAlgorithmResult<TGenotype>? algorithmResult) {
    // ToDo: "Iteration" looks like a operator
    return new GeneticAlgorithmResult<TGenotype>() {
      CurrentGeneration = iterationResult.Generation,
      TotalGenerations = iterationResult.Generation,
      CurrentDuration = iterationResult.Duration,
      TotalDuration = iterationResult.Duration + (algorithmResult?.TotalDuration ?? TimeSpan.Zero),
      CurrentOperatorMetrics = iterationResult.OperatorMetrics,
      TotalOperatorMetrics = iterationResult.OperatorMetrics + (algorithmResult?.TotalOperatorMetrics ?? new GeneticAlgorithmOperatorMetrics()),
      Objective = iterationResult.Objective,
      CurrentPopulation = iterationResult.Population,
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
