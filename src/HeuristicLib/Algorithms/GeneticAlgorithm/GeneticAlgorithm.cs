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
  //public ITerminator<GeneticAlgorithmResult<TGenotype>> Terminator { get; }
  public IInterceptor<GeneticAlgorithmIterationResult<TGenotype>> Interceptor { get; }
  
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
    //Terminator = terminator;
    Interceptor = interceptor ?? Interceptors.Identity<GeneticAlgorithmIterationResult<TGenotype>>();
  }


  // public override EvaluatedIndividual<TGenotype> Solve<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, GeneticAlgorithmState<TGenotype>? initialState = null) {
  //   var lastState = Execute(problem, initialState);
  //   return lastState.BestSolution;
  // }
  
  // public override IEnumerable<EvaluatedIndividual<TGenotype>> SolveStreaming<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, GeneticAlgorithmState<TGenotype>? initialState = null) {
  //   foreach (var state in ExecuteStreaming(problem, initialState)) {
  //     yield return state.BestSolution;
  //   }
  // }
  
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
    //
    // var givenPopulation = initializationState.Population;
    // int remainingCount = PopulationSize - givenPopulation.Length;

    var startCreating = Stopwatch.GetTimestamp();
    // var newPopulation = Enumerable.Range(0, remainingCount).Select(i => Creator.Create(problem.Encoding, random)).ToArray();
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
    
    var population = Population.From(genotypePopulation, phenotypePopulation, fitnesses);
    
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
  
  // public override EvaluatedIndividual<TGenotype, TPhenotype> GetBestSolution<TPhenotype>(GeneticAlgorithmResult<TGenotype, TPhenotype> result, IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem) {
  //   return result.BestPhenotypeSolution;
  // }

  // public virtual EvaluatedIndividual<TGenotype/*, TPhenotype*/> GetBestSolution<TPhenotype>(GeneticAlgorithmResult<TGenotype> state, IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem) {
  //   return state.BestSolution;
  // }
  
  
  // public override EvolutionResult<TGenotype, TPhenotype> ExecuteInitialization(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem) {
  //   return ExecuteInitialization(problem, new UnevaluatedPopulation<TGenotype> { Population = [], Generation = 0 });
  // }
  
  // public EvolutionResult<TGenotype, TPhenotype> ExecuteInitialization(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, UnevaluatedPopulation<TGenotype> initializationState) {
  //   var start = Stopwatch.GetTimestamp();
  //
  //   var random = RandomSource.CreateRandomNumberGenerator();
  //   
  //   var givenPopulation = initializationState.Population;
  //   int remainingCount = PopulationSize - givenPopulation.Length;
  //
  //   var startCreating = Stopwatch.GetTimestamp();
  //   var newPopulation = Enumerable.Range(0, remainingCount).Select(i => Creator.Create(problem.Encoding, random)).ToArray();
  //   var endCreating = Stopwatch.GetTimestamp();
  //   
  //   var genotypePopulation = givenPopulation.Concat(newPopulation).Take(PopulationSize).ToArray();
  //   
  //   var startDecoding = Stopwatch.GetTimestamp();
  //   var phenotypePopulation = genotypePopulation.Select(genotype => problem.Decoder.Decode(genotype)).ToArray();
  //   var endDecoding = Stopwatch.GetTimestamp();
  //   
  //   var startEvaluating = Stopwatch.GetTimestamp();
  //   var fitnesses = phenotypePopulation.Select(phenotype => problem.Evaluator.Evaluate(phenotype)).ToArray();
  //   var endEvaluating = Stopwatch.GetTimestamp();
  //
  //   var population = Population.From(genotypePopulation, phenotypePopulation, fitnesses);
  //   
  //   var endBeforeInterceptor = Stopwatch.GetTimestamp();
  //   
  //   var result = new EvolutionResult<TGenotype, TPhenotype>() {
  //     Generation = initializationState.Generation,
  //     Objective = problem.Objective,
  //     Population = population,
  //     CreationCount = remainingCount,
  //     DecodingCount = phenotypePopulation.Length,
  //     EvaluationCount = fitnesses.Length,
  //     CreationDuration = Stopwatch.GetElapsedTime(startCreating, endCreating),
  //     DecodingDuration = Stopwatch.GetElapsedTime(startDecoding, endDecoding),
  //     EvaluationDuration = Stopwatch.GetElapsedTime(startEvaluating, endEvaluating),
  //     TotalDuration = Stopwatch.GetElapsedTime(start, endBeforeInterceptor)
  //   };
  //   
  //   var interceptorStart = Stopwatch.GetTimestamp();
  //   var interceptedResult = Interceptor.Transform(result);
  //   var interceptorEnd = Stopwatch.GetTimestamp();
  //
  //   var end = Stopwatch.GetTimestamp();
  //   return interceptedResult with {
  //     TotalDuration = Stopwatch.GetElapsedTime(start, end),
  //     InterceptionDuration = Stopwatch.GetElapsedTime(interceptorStart, interceptorEnd)
  //   };
  // }
  
  // public override EvolutionResult<TGenotype, TPhenotype> ExecuteIteration(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, EvaluatedPopulation<TGenotype, TPhenotype> continuationState) {
  //   var start = Stopwatch.GetTimestamp();
  //   
  //   var random = RandomSource.CreateRandomNumberGenerator();
  //   
  //   int offspringCount = Replacer.GetOffspringCount(PopulationSize);
  //
  //   var oldPopulation = continuationState.Population;
  //   
  //   var startSelection = Stopwatch.GetTimestamp();
  //   var parents = Selector.Select(oldPopulation, problem.Objective, offspringCount * 2, random).ToList();
  //   var endSelection = Stopwatch.GetTimestamp();
  //
  //    
  //   var genotypePopulation = new TGenotype[offspringCount];
  //   var startCrossover = Stopwatch.GetTimestamp();
  //   int crossoverCount = 0;
  //   for (int i = 0; i < parents.Count; i += 2) {
  //     var parent1 = parents[i];
  //     var parent2 = parents[i + 1];
  //     var child = Crossover.Cross(parent1.Genotype, parent2.Genotype, problem.Encoding, random);
  //     genotypePopulation[i / 2] = child;
  //     crossoverCount++;
  //   }
  //   var endCrossover = Stopwatch.GetTimestamp();
  //   
  //   var startMutation = Stopwatch.GetTimestamp();
  //   int mutationCount = 0;
  //   for (int i = 0; i < genotypePopulation.Length; i++) {
  //     if (random.Random() < MutationRate) {
  //       genotypePopulation[i] = Mutator.Mutate(genotypePopulation[i], problem.Encoding, random);
  //       mutationCount++;
  //     }
  //   }
  //   var endMutation = Stopwatch.GetTimestamp();
  //   
  //   var startDecoding = Stopwatch.GetTimestamp();
  //   var phenotypePopulation = genotypePopulation.Select(genotype => problem.Decoder.Decode(genotype)).ToArray();
  //   var endDecoding = Stopwatch.GetTimestamp();
  //   
  //   var startEvaluation = Stopwatch.GetTimestamp();
  //   var fitnesses = phenotypePopulation.Select(phenotype => problem.Evaluator.Evaluate(phenotype)).ToArray();
  //   var endEvaluation = Stopwatch.GetTimestamp();
  //   
  //   var population = Population.From(genotypePopulation, phenotypePopulation, fitnesses);
  //   
  //   var startReplacement = Stopwatch.GetTimestamp();
  //   var newPopulation = Replacer.Replace(oldPopulation, population, problem.Objective, random);
  //   var endReplacement = Stopwatch.GetTimestamp();
  //   
  //   var endBeforeInterceptor = Stopwatch.GetTimestamp();
  //
  //   var result = new EvolutionResult<TGenotype, TPhenotype>() {
  //     Generation = continuationState.Generation,
  //     Objective = problem.Objective,
  //     Population = newPopulation,
  //     SelectionCount = parents.Count,
  //     CrossoverCount = crossoverCount,
  //     MutationCount = mutationCount,
  //     DecodingCount = phenotypePopulation.Length,
  //     EvaluationCount = fitnesses.Length,
  //     SelectionDuration = Stopwatch.GetElapsedTime(startSelection, endSelection),
  //     CrossoverDuration = Stopwatch.GetElapsedTime(startCrossover, endCrossover),
  //     MutationDuration = Stopwatch.GetElapsedTime(startMutation, endMutation),
  //     DecodingDuration = Stopwatch.GetElapsedTime(startDecoding, endDecoding),
  //     EvaluationDuration = Stopwatch.GetElapsedTime(startEvaluation, endEvaluation),
  //     ReplacementDuration = Stopwatch.GetElapsedTime(startReplacement, endReplacement),
  //     TotalDuration = Stopwatch.GetElapsedTime(start, endBeforeInterceptor)
  //   };
  //   
  //   var interceptorStart = Stopwatch.GetTimestamp();
  //   var interceptedResult = Interceptor.Transform(result);
  //   var interceptorEnd = Stopwatch.GetTimestamp();
  //   
  //   var end = Stopwatch.GetTimestamp();
  //   
  //   return interceptedResult with {
  //     TotalDuration = Stopwatch.GetElapsedTime(start, end),
  //     InterceptionDuration = Stopwatch.GetElapsedTime(interceptorStart, interceptorEnd)
  //   };
  // }
  
  
  
  #region base-class
  //
  // public virtual EvaluatedIndividual<TGenotype/*, TPhenotype*/> Solve<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, GeneticAlgorithmState<TGenotype>? initialState = null) {
  //   var lastState = Execute(problem, initialState);
  //   //return GetBestSolution(lastState, problem);
  //   return lastState.BestSolution;
  // }
  //
  // public virtual IEnumerable<EvaluatedIndividual<TGenotype/*, TPhenotype*/>> SolveStreaming<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, GeneticAlgorithmState<TGenotype>? initialState = null) {
  //   foreach (var state in ExecuteStreaming(problem, initialState)) {
  //     //yield return GetBestSolution(state, problem);
  //     yield return state.BestSolution;
  //   }
  // }
  //
  // public virtual GeneticAlgorithmResult<TGenotype> Execute<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, GeneticAlgorithmState<TGenotype>? initialState = null) {
  //   return ExecuteStreaming(problem, initialState).Last();
  // }
  //
  // public virtual IEnumerable<GeneticAlgorithmResult<TGenotype>> ExecuteStreaming<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, GeneticAlgorithmState<TGenotype>? initialState = null) {
  //   var currentResult = initialState is null
  //     ? ExecuteInitialization(problem) 
  //     : ExecuteIteration(initialState, problem);
  //   yield return currentResult;
  //
  //   while (Terminator.ShouldContinue(currentResult)) {
  //     var currentState = currentResult.GetNextState();
  //     currentResult = ExecuteIteration(currentState, problem);
  //     yield return currentResult;
  //   }
  // }
  
  
  // public virtual EvaluatedIndividual<TGenotype, TPhenotype> Solve<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, TState? initialState = null) {
  //   var lastState = Execute(problem, initialState);
  //   return GetBestSolution(lastState, problem);
  // }
  //
  // public virtual IEnumerable<EvaluatedIndividual<TGenotype, TPhenotype>> SolveStreaming<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, TState? initialState = null) {
  //   foreach (var state in ExecuteStreaming(problem, initialState)) {
  //     yield return GetBestSolution(state, problem);
  //   }
  // }
  //
  // public virtual TResult Execute<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, TState? initialState = null) {
  //   return ExecuteStreaming(problem, initialState).Last();
  // }
  //
  // public virtual IEnumerable<TResult> ExecuteStreaming<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, TState? initialState = null) {
  //   var currentResult = initialState is null
  //     ? ExecuteInitialization(problem) 
  //     : ExecuteIteration(problem, initialState);
  //   yield return currentResult;
  //
  //   while (Terminator.ShouldContinue(currentResult)) {
  //     var currentState = currentResult.GetNextState();
  //     currentResult = ExecuteIteration(problem, currentState);
  //     yield return currentResult;
  //   }
  // }
  #endregion
  
  
  // public override ResultStream<PopulationState<TGenotype>> CreateExecutionStream(IRandomNumberGenerator random, PopulationState<TGenotype>? initialState = null) {
  //   return new ResultStream<PopulationState<TGenotype>>(InternalCreateExecutionStream(random, initialState));
  // }
  //
  // private IEnumerable<PopulationState<TGenotype>> InternalCreateExecutionStream(IRandomNumberGenerator random, PopulationState<TGenotype>? initialState) {
  //   //var random = RandomSource.CreateRandomNumberGenerator();
  //   
  //   int offspringCount = Replacer.GetOffspringCount(PopulationSize);
  //
  //   PopulationState<TGenotype> currentState;
  //   if (initialState is not null) {
  //     currentState = initialState;
  //   } else {
  //     var initialPopulation = InitializePopulation();
  //     var evaluatedPopulation = Evaluator.Evaluate(initialPopulation);
  //     currentState = new PopulationState<TGenotype> { Population = evaluatedPopulation, Objective = Objective };
  //     currentState = Interceptor.Transform(currentState);
  //     yield return currentState;
  //   }
  //
  //   while (Terminator?.ShouldContinue(currentState) ?? true) {
  //     var offsprings = EvolvePopulation(currentState.Population, offspringCount, random);
  //     var evaluatedOffsprings = Evaluator.Evaluate(offsprings);
  //
  //     var newPopulation = Replacer.Replace(currentState.Population, evaluatedOffsprings, Objective);
  //
  //     currentState = currentState.Next() with { Population = newPopulation }; // increment durations and other counts
  //     currentState = Interceptor.Transform(currentState);
  //
  //     yield return currentState;
  //   }
  // }
  //
  // private TGenotype[] InitializePopulation() {
  //   var population = new TGenotype[PopulationSize];
  //   for (int i = 0; i < PopulationSize; i++) {
  //     population[i] = Creator.Create();
  //   }
  //   return population;
  // }
  //
  // private TGenotype[] EvolvePopulation(Solution<TPhenotype>[] population, int offspringCount, IRandomNumberGenerator random) {
  //   var newPopulation = new TGenotype[offspringCount];
  //   var parents = Selector.Select(population, Objective, offspringCount * 2, random)/*.Select(p => p.Phenotype)*/.ToList();
  //
  //   for (int i = 0; i < parents.Count; i += 2) {
  //     var parent1 = parents[i];
  //     var parent2 = parents[i + 1];
  //     var offspring = Crossover.Cross(parent1.Genotype, parent2.Genotype, Encoding, random);
  //     if (random.Random() < MutationRate) {
  //       offspring = Mutator.Mutate(offspring, Encoding, random);
  //     }
  //     newPopulation[i / 2] = offspring;
  //   }
  //   return newPopulation;
  // }


}

public static class GeneticAlgorithm {
  public static GeneticAlgorithm<TGenotype, TEncoding> FromConfiguration<TGenotype, TEncoding>(
    GeneticAlgorithmConfiguration<TGenotype, TEncoding> configuration)
    where TEncoding : IEncoding<TGenotype> 
  {
    return configuration.Build();
  }
}
