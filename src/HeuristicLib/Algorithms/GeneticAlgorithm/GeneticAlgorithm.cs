using System.Diagnostics;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;

public record class GeneticAlgorithm<TGenotype, TSearchSpace, TProblem>
  : IterativeAlgorithm<TGenotype, TSearchSpace, TProblem, GeneticAlgorithmState<TGenotype>, GeneticAlgorithmIterationResult<TGenotype>, GeneticAlgorithmResult<TGenotype>>
  where TSearchSpace : ISearchSpace<TGenotype> 
  where TProblem : IOptimizable<TGenotype, TSearchSpace>
{
  public int PopulationSize { get; init; }
  public Creator<TGenotype, TSearchSpace, TProblem> Creator { get; init; }
  public Crossover<TGenotype, TSearchSpace, TProblem> Crossover { get; init; }
  public Mutator<TGenotype, TSearchSpace, TProblem> Mutator { get; init; }
  public double MutationRate { get; init; }
  public Selector<TGenotype, TSearchSpace, TProblem> Selector { get; init; }
  public Replacer<TGenotype, TSearchSpace, TProblem> Replacer { get; init; }
  public int RandomSeed { get; init; }
  // ToDo: Interceptor could also be defined on AlgorithmResult and handled at the IterativeAlgorithm level.
  public Interceptor<TGenotype, TSearchSpace, TProblem, GeneticAlgorithmIterationResult<TGenotype>>? Interceptor { get; init; }

  public GeneticAlgorithm(
    int populationSize,
    Creator<TGenotype, TSearchSpace, TProblem> creator,
    Crossover<TGenotype, TSearchSpace, TProblem> crossover,
    Mutator<TGenotype, TSearchSpace, TProblem> mutator, double mutationRate,
    Selector<TGenotype, TSearchSpace, TProblem> selector, Replacer<TGenotype, TSearchSpace, TProblem> replacer,
    int randomSeed,
    Terminator<TGenotype, TSearchSpace, TProblem, GeneticAlgorithmResult<TGenotype>> terminator,
    Interceptor<TGenotype, TSearchSpace, TProblem, GeneticAlgorithmIterationResult<TGenotype>>? interceptor = null
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

  public override GeneticAlgorithmExecution<TGenotype, TSearchSpace, TProblem> CreateStreamingExecution(TProblem optimizable) {
    return new GeneticAlgorithmExecution<TGenotype, TSearchSpace, TProblem>(this, optimizable);
  }
}

public record class GeneticAlgorithm<TGenotype, TSearchSpace> : GeneticAlgorithm<TGenotype, TSearchSpace, IOptimizable<TGenotype, TSearchSpace>> 
  where TSearchSpace : ISearchSpace<TGenotype>
{
  public GeneticAlgorithm(
    int populationSize,
    Creator<TGenotype, TSearchSpace> creator,
    Crossover<TGenotype, TSearchSpace> crossover,
    Mutator<TGenotype, TSearchSpace> mutator, double mutationRate,
    Selector<TGenotype, TSearchSpace> selector, Replacer<TGenotype, TSearchSpace> replacer,
    int randomSeed,
    Terminator<TGenotype, TSearchSpace, GeneticAlgorithmResult<TGenotype>> terminator,
    Interceptor<TGenotype, TSearchSpace, GeneticAlgorithmIterationResult<TGenotype>>? interceptor = null
  ) : base(populationSize, creator, crossover, mutator, mutationRate, selector, replacer, randomSeed, terminator, interceptor) { }
}

public class GeneticAlgorithmExecution<TGenotype, TSearchSpace, TProblem>
  : IterativeAlgorithmExecution<TGenotype, TSearchSpace, TProblem, GeneticAlgorithmState<TGenotype>, GeneticAlgorithmIterationResult<TGenotype>, GeneticAlgorithmResult<TGenotype>, GeneticAlgorithm<TGenotype, TSearchSpace, TProblem>>
  where TSearchSpace : ISearchSpace<TGenotype> 
  where TProblem : IOptimizable<TGenotype, TSearchSpace>
{
  public IRandomNumberGenerator Random { get; }
  public ICreatorExecution<TGenotype> Creator { get; }
  public ICrossoverExecution<TGenotype> Crossover { get; }
  public IMutatorExecution<TGenotype> Mutator { get; }
  public ISelectorExecution<TGenotype> Selector { get; }
  public IReplacerInstance<TGenotype> Replacer { get; }
  public IInterceptorExecution<GeneticAlgorithmIterationResult<TGenotype>>? Interceptor { get; }
  
  public GeneticAlgorithmExecution(GeneticAlgorithm<TGenotype, TSearchSpace, TProblem> parameters, TProblem problem) : base(parameters, problem) {
    Random = new SystemRandomNumberGenerator(parameters.RandomSeed);
    Creator = parameters.Creator.CreateExecution(problem.SearchSpace, problem);
    Crossover = parameters.Crossover.CreateExecution(problem.SearchSpace, problem);
    Mutator = parameters.Mutator.CreateExecution(problem.SearchSpace, problem);
    Selector = parameters.Selector.CreateExecution(problem.SearchSpace, problem);
    Replacer = parameters.Replacer.CreateExecution(problem.SearchSpace, problem);
    Interceptor = parameters.Interceptor?.CreateExecution(problem.SearchSpace, problem);
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
  
  protected override GeneticAlgorithmIterationResult<TGenotype> ExecuteInitialization() {
    var start = Stopwatch.GetTimestamp();

    var startCreating = Stopwatch.GetTimestamp();
    var genotypes = Enumerable.Range(0, Parameters.PopulationSize).Select(i => Creator.Create(Random)).ToArray();
    var endCreating = Stopwatch.GetTimestamp();
    
    //var genotypePopulation = newPopulation;
    
    // var startDecoding = Stopwatch.GetTimestamp();
    // var phenotypePopulation = genotypePopulation.Select(genotype => optimizable.Decoder.Decode(genotype)).ToArray();
    // var endDecoding = Stopwatch.GetTimestamp();
    
    var startEvaluating = Stopwatch.GetTimestamp();
    // var fitnesses = phenotypePopulation.Select(phenotype => optimizable.Evaluator.Evaluate(phenotype)).ToArray();
    var fitnesses = genotypes.Select(genotype => Problem.Evaluate(genotype)).ToArray();
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
      Objective = Problem.Objective
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
  
  protected override GeneticAlgorithmIterationResult<TGenotype> ExecuteIteration(GeneticAlgorithmState<TGenotype> state) {
    var start = Stopwatch.GetTimestamp();
    
    // int newRandomSeed = SeedSequence.GetSeed(Algorithm.RandomSeed, state.Generation);
    // var random = new SystemRandomNumberGenerator(newRandomSeed);
    
    int offspringCount = Replacer.GetOffspringCount(Parameters.PopulationSize);

    var oldPopulation = state.Population;
    
    var startSelection = Stopwatch.GetTimestamp();
    var parents = Selector.Select(oldPopulation, Problem.Objective, offspringCount * 2, Random).ToList();
    var endSelection = Stopwatch.GetTimestamp();
     
    var genotypes = new TGenotype[offspringCount];
    var startCrossover = Stopwatch.GetTimestamp();
    int crossoverCount = 0;
    for (int i = 0; i < parents.Count; i += 2) {
      var parent1 = parents[i];
      var parent2 = parents[i + 1];
      var child = Crossover.Cross(parent1.Genotype, parent2.Genotype, Random);
      genotypes[i / 2] = child;
      crossoverCount++;
    }
    var endCrossover = Stopwatch.GetTimestamp();
    
    var startMutation = Stopwatch.GetTimestamp();
    int mutationCount = 0;
    for (int i = 0; i < genotypes.Length; i++) {
      if (Random.Random() < Parameters.MutationRate) {
        genotypes[i] = Mutator.Mutate(genotypes[i], Random);
        mutationCount++;
      }
    }
    var endMutation = Stopwatch.GetTimestamp();
    
    // var startDecoding = Stopwatch.GetTimestamp();
    // var phenotypePopulation = genotypePopulation.Select(genotype => optimizable.Decoder.Decode(genotype)).ToArray();
    // var endDecoding = Stopwatch.GetTimestamp();
    
    var startEvaluation = Stopwatch.GetTimestamp();
    var fitnesses = genotypes.Select(genotype => Problem.Evaluate(genotype)).ToArray();
    var endEvaluation = Stopwatch.GetTimestamp();
    
    var population = Population.From(genotypes, /*phenotypePopulation,*/ fitnesses);
    
    var startReplacement = Stopwatch.GetTimestamp();
    var newPopulation = Replacer.Replace(oldPopulation, population, Problem.Objective, Random);
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
      Objective = Problem.Objective
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

  protected override bool IsValidState(GeneticAlgorithmState<TGenotype> state) {
    return base.IsValidState(state)
      && state.Population.All(individual => Problem.SearchSpace.Contains(individual.Genotype));
  }
}

public static class GeneticAlgorithm {
  public static GeneticAlgorithm<TGenotype, TSearchSpace, TProblem> FromConfiguration<TGenotype, TSearchSpace, TProblem>(
    GeneticAlgorithmConfiguration<TGenotype, TSearchSpace, TProblem> configuration)
    where TSearchSpace : ISearchSpace<TGenotype> 
    where TProblem : IOptimizable<TGenotype, TSearchSpace>
  {
    return configuration.Build();
  }
}
