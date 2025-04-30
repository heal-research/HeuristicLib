using System.Diagnostics;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;

public record class GeneticAlgorithm<TGenotype, TSearchSpace>
  : IterativeAlgorithm<TGenotype, TSearchSpace, GeneticAlgorithmState<TGenotype>, GeneticAlgorithmIterationResult<TGenotype>, GeneticAlgorithmResult<TGenotype>>
  where TSearchSpace : ISearchSpace<TGenotype> 
{
  public int PopulationSize { get; init; }
  public Creator<TGenotype, TSearchSpace> Creator { get; init; }
  public Crossover<TGenotype, TSearchSpace> Crossover { get; init; }
  public Mutator<TGenotype, TSearchSpace> Mutator { get; init; }
  public double MutationRate { get; init; }
  public Selector Selector { get; init; }
  public Replacer Replacer { get; init; }
  public int RandomSeed { get; init; }
  // ToDo: Interceptor could also be defined on AlgorithmResult and handled at the IterativeAlgorithm level.
  public Interceptor<GeneticAlgorithmIterationResult<TGenotype>>? Interceptor { get; init; }

  public GeneticAlgorithm(
    int populationSize,
    Creator<TGenotype, TSearchSpace> creator,
    Crossover<TGenotype, TSearchSpace> crossover,
    Mutator<TGenotype, TSearchSpace> mutator, double mutationRate,
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

  public override GeneticAlgorithmInstance<TGenotype, TSearchSpace> CreateInstance() {
    return new GeneticAlgorithmInstance<TGenotype, TSearchSpace>(this);
  }
}

// public interface IGeneticAlgorithmInstance<TGenotype, TSearchSpace> {
//   
// }

public class GeneticAlgorithmInstance<TGenotype, TSearchSpace>
  : IterativeAlgorithmInstance<TGenotype, TSearchSpace, GeneticAlgorithmState<TGenotype>, GeneticAlgorithmIterationResult<TGenotype>, GeneticAlgorithmResult<TGenotype>, GeneticAlgorithm<TGenotype, TSearchSpace>>
  where TSearchSpace : ISearchSpace<TGenotype> 
{
  public IRandomNumberGenerator Random { get; }
  public ICreatorInstance<TGenotype, TSearchSpace> Creator { get; }
  public ICrossoverInstance<TGenotype, TSearchSpace> Crossover { get; }
  public IMutatorInstance<TGenotype, TSearchSpace> Mutator { get; }
  public ISelectorInstance Selector { get; }
  public IReplacerInstance Replacer { get; }
  public IInterceptorInstance<GeneticAlgorithmIterationResult<TGenotype>>? Interceptor { get; }
  
  public GeneticAlgorithmInstance(GeneticAlgorithm<TGenotype, TSearchSpace> parameters) : base(parameters) {
    Random = new SystemRandomNumberGenerator(parameters.RandomSeed);
    Creator = parameters.Creator.CreateInstance();
    Crossover = parameters.Crossover.CreateInstance();
    Mutator = parameters.Mutator.CreateInstance();
    Selector = parameters.Selector.CreateInstance();
    Replacer = parameters.Replacer.CreateInstance();
    Interceptor = parameters.Interceptor?.CreateInstance();
  }

  // public override GeneticAlgorithmResult<TGenotype> Execute<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TSearchSpace> problem, GeneticAlgorithmState<TGenotype>? initialState = null) {
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
  
  protected override GeneticAlgorithmIterationResult<TGenotype> ExecuteInitialization(IOptimizable<TGenotype, TSearchSpace> optimizable) {
    var start = Stopwatch.GetTimestamp();

    var startCreating = Stopwatch.GetTimestamp();
    var genotypes = Enumerable.Range(0, Parameters.PopulationSize).Select(i => Creator.Create(optimizable.SearchSpace, Random)).ToArray();
    var endCreating = Stopwatch.GetTimestamp();
    
    //var genotypePopulation = newPopulation;
    
    // var startDecoding = Stopwatch.GetTimestamp();
    // var phenotypePopulation = genotypePopulation.Select(genotype => optimizable.Decoder.Decode(genotype)).ToArray();
    // var endDecoding = Stopwatch.GetTimestamp();
    
    var startEvaluating = Stopwatch.GetTimestamp();
    // var fitnesses = phenotypePopulation.Select(phenotype => optimizable.Evaluator.Evaluate(phenotype)).ToArray();
    var fitnesses = genotypes.Select(genotype => optimizable.Evaluate(genotype)).ToArray();
    var endEvaluating = Stopwatch.GetTimestamp();

    var population = Population.From(genotypes, /*phenotypePopulation,*/ fitnesses);
    
    var endBeforeInterceptor = Stopwatch.GetTimestamp();
    
    var result = new GeneticAlgorithmIterationResult<TGenotype>() {
      Generation = 0,
      Duration = Stopwatch.GetElapsedTime(start, endBeforeInterceptor),
      OperatorMetrics = new GeneticAlgorithmOperatorMetrics {
        Creation = new OperatorMetric(genotypes.Length, Stopwatch.GetElapsedTime(startCreating, endCreating)),
        // Decoding = new OperatorMetric(phenotypePopulation.Length, Stopwatch.GetElapsedTime(startDecoding, endDecoding)),
        Evaluation = new OperatorMetric(fitnesses.Length, Stopwatch.GetElapsedTime(startEvaluating, endEvaluating)),
      },
      Population = population,
      Objective = optimizable.Objective
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
  
  protected override GeneticAlgorithmIterationResult<TGenotype> ExecuteIteration(IOptimizable<TGenotype, TSearchSpace> optimizable, GeneticAlgorithmState<TGenotype> state) {
    var start = Stopwatch.GetTimestamp();
    
    // int newRandomSeed = SeedSequence.GetSeed(Algorithm.RandomSeed, state.Generation);
    // var random = new SystemRandomNumberGenerator(newRandomSeed);
    
    int offspringCount = Replacer.GetOffspringCount(Parameters.PopulationSize);

    var oldPopulation = state.Population;
    
    var startSelection = Stopwatch.GetTimestamp();
    var parents = Selector.Select(oldPopulation, optimizable.Objective, offspringCount * 2, Random).ToList();
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
    // var phenotypePopulation = genotypePopulation.Select(genotype => optimizable.Decoder.Decode(genotype)).ToArray();
    // var endDecoding = Stopwatch.GetTimestamp();
    
    var startEvaluation = Stopwatch.GetTimestamp();
    var fitnesses = genotypes.Select(genotype => optimizable.Evaluate(genotype)).ToArray();
    var endEvaluation = Stopwatch.GetTimestamp();
    
    var population = Population.From(genotypes, /*phenotypePopulation,*/ fitnesses);
    
    var startReplacement = Stopwatch.GetTimestamp();
    var newPopulation = Replacer.Replace(oldPopulation, population, optimizable.Objective, Random);
    var endReplacement = Stopwatch.GetTimestamp();
    
    var endBeforeInterceptor = Stopwatch.GetTimestamp();
    
    var result = new GeneticAlgorithmIterationResult<TGenotype>() {
      Generation = state.Generation + 1,
      Duration = Stopwatch.GetElapsedTime(start, endBeforeInterceptor),
      OperatorMetrics = new GeneticAlgorithmOperatorMetrics() {
        Selection = new OperatorMetric(parents.Count, Stopwatch.GetElapsedTime(startSelection, endSelection)),
        Crossover = new OperatorMetric(crossoverCount, Stopwatch.GetElapsedTime(startCrossover, endCrossover)),
        Mutation = new OperatorMetric(mutationCount, Stopwatch.GetElapsedTime(startMutation, endMutation)),
        // Decoding = new OperatorMetric(phenotypePopulation.Length, Stopwatch.GetElapsedTime(startDecoding, endDecoding)),
        Evaluation = new OperatorMetric(fitnesses.Length, Stopwatch.GetElapsedTime(startEvaluation, endEvaluation)),
        Replacement = new OperatorMetric(1, Stopwatch.GetElapsedTime(startReplacement, endReplacement))
      },
      Population = newPopulation,
      Objective = optimizable.Objective
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
    var currentBestSolution = iterationResult.Population.MinBy(x => x.Fitness, iterationResult.Objective.TotalOrderComparer);
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
      CurrentBestSolution = currentBestSolution,
      BestSolution = algorithmResult is null ? currentBestSolution : new[] { algorithmResult.BestSolution, currentBestSolution }.MinBy(x => x.Fitness, iterationResult.Objective.TotalOrderComparer)};
  }

  protected override bool IsValidState(GeneticAlgorithmState<TGenotype> state, IOptimizable<TGenotype, TSearchSpace> optimizable) {
    return base.IsValidState(state, optimizable)
      && state.Population.All(individual => optimizable.SearchSpace.Contains(individual.Genotype));
  }
}

public static class GeneticAlgorithm {
  public static GeneticAlgorithm<TGenotype, TSearchSpace> FromConfiguration<TGenotype, TSearchSpace>(
    GeneticAlgorithmConfiguration<TGenotype, TSearchSpace> configuration)
    where TSearchSpace : ISearchSpace<TGenotype> 
  {
    return configuration.Build();
  }
}
