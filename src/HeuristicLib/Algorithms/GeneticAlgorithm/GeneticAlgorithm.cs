using System.Diagnostics;
using HEAL.HeuristicLib.Core;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;

public record class GeneticAlgorithm<TGenotype, TPhenotype, TEncoding>
  : IContinuableAlgorithm<
      GenotypeStartPopulation<TGenotype>, 
      PhenotypeStartPopulation<TGenotype, TPhenotype>,
      EvolutionResult<TGenotype, TPhenotype>
  >
  where TEncoding : IEncoding<TGenotype>
{
  public required TEncoding Encoding { get; init; }
  public required int PopulationSize { get; init;  }
  public required ICreator<TGenotype, TEncoding> Creator { get; init; }
  public required ICrossover<TGenotype, TEncoding> Crossover { get; init; }
  public required IMutator<TGenotype, TEncoding> Mutator { get; init; }
  public required double MutationRate { get; init; }
  public required IDecoder<TGenotype, TPhenotype> Decoder { get; init; }
  public required IEvaluator<TPhenotype> Evaluator { get; init; }
  public required Objective Objective { get; init; }
  public required IRandomSource RandomSource { get; init; }
  public required ISelector Selector { get; init; }
  public required IReplacer Replacer { get; init; }
  //public ITerminator<PopulationState<TGenotype>>? Terminator { get; }
  public IInterceptor<EvolutionResult<TGenotype, TPhenotype>> Interceptor { get; init; } = Interceptors.Identity<EvolutionResult<TGenotype, TPhenotype>>();

  // public GeneticAlgorithm() { }
  // public GeneticAlgorithm(
  //   TEncoding encoding,
  //   int populationSize,
  //   ICreator<TGenotype, TEncoding> creator,
  //   ICrossover<TGenotype, TEncoding> crossover, 
  //   IMutator<TGenotype, TEncoding> mutator, double mutationRate,
  //   IDecoder<TGenotype, TPhenotype> decoder, IEvaluator<TPhenotype> evaluator, Objective objective,
  //   ISelector selector, IReplacer replacer,
  //   IRandomSource randomSourceState,
  //   //ITerminator<PopulationState<TGenotype>>? terminator = null,
  //   IInterceptor<EvolutionResult<TGenotype, TPhenotype>>? interceptor = null)
  // {
  //   Encoding = encoding;
  //   PopulationSize = populationSize;
  //   Creator = creator;
  //   Crossover = crossover;
  //   Mutator = mutator;
  //   MutationRate = mutationRate;
  //   Decoder = decoder;
  //   Evaluator = evaluator;
  //   Objective = objective;
  //   Selector = selector;
  //   Replacer = replacer;
  //   RandomSource = randomSourceState;
  //   //Terminator = terminator;
  //   Interceptor = interceptor ?? Interceptors.Identity<EvolutionResult<TGenotype, TPhenotype>>();
  // }
  
  public virtual EvolutionResult<TGenotype, TPhenotype> Execute(GenotypeStartPopulation<TGenotype>? startState = null) {
    var start = Stopwatch.GetTimestamp();

    var random = RandomSource.CreateRandomNumberGenerator();
    
    var givenPopulation = startState?.Population ?? [];
    int remainingCount = PopulationSize - givenPopulation.Length;

    var startCreating = Stopwatch.GetTimestamp();
    var newPopulation = Enumerable.Range(0, remainingCount).Select(i => Creator.Create(Encoding, random)).ToArray();
    var endCreating = Stopwatch.GetTimestamp();
    
    var genotypePopulation = givenPopulation.Concat(newPopulation).Take(PopulationSize).ToArray();
    
    var startDecoding = Stopwatch.GetTimestamp();
    var phenotypePopulation = genotypePopulation.Select(genotype => Decoder.Decode(genotype)).ToArray();
    var endDecoding = Stopwatch.GetTimestamp();
    
    var startEvaluating = Stopwatch.GetTimestamp();
    var fitnesses = phenotypePopulation.Select(phenotype => Evaluator.Evaluate(phenotype)).ToArray();
    var endEvaluating = Stopwatch.GetTimestamp();

    var population = Population.From(genotypePopulation, phenotypePopulation, fitnesses);
    
    var endBeforeInterceptor = Stopwatch.GetTimestamp();
    
    var result = new EvolutionResult<TGenotype, TPhenotype>() {
      Generation = startState?.Generation ?? 0,
      Objective = Objective,
      Population = population,
      CreationCount = remainingCount,
      DecodingCount = phenotypePopulation.Length,
      EvaluationCount = fitnesses.Length,
      CreationDuration = Stopwatch.GetElapsedTime(startCreating, endCreating),
      DecodingDuration = Stopwatch.GetElapsedTime(startDecoding, endDecoding),
      EvaluationDuration = Stopwatch.GetElapsedTime(startEvaluating, endEvaluating),
      TotalDuration = Stopwatch.GetElapsedTime(start, endBeforeInterceptor)
    };
    
    var interceptorStart = Stopwatch.GetTimestamp();
    var interceptedResult = Interceptor.Transform(result);
    var interceptorEnd = Stopwatch.GetTimestamp();

    var end = Stopwatch.GetTimestamp();
    return interceptedResult with {
      TotalDuration = Stopwatch.GetElapsedTime(start, end),
      InterceptionDuration = Stopwatch.GetElapsedTime(interceptorStart, interceptorEnd)
    };
  }
  
  public virtual EvolutionResult<TGenotype, TPhenotype> Execute(PhenotypeStartPopulation<TGenotype, TPhenotype> continuationState) {
    var start = Stopwatch.GetTimestamp();
    
    var random = RandomSource.CreateRandomNumberGenerator();
    
    int offspringCount = Replacer.GetOffspringCount(PopulationSize);

    var oldPopulation = continuationState.Population;
    
    var startSelection = Stopwatch.GetTimestamp();
    var parents = Selector.Select(oldPopulation, Objective, offspringCount * 2, random).ToList();
    var endSelection = Stopwatch.GetTimestamp();

     
    var genotypePopulation = new TGenotype[offspringCount];
    var startCrossover = Stopwatch.GetTimestamp();
    int crossoverCount = 0;
    for (int i = 0; i < parents.Count; i += 2) {
      var parent1 = parents[i];
      var parent2 = parents[i + 1];
      var child = Crossover.Cross(parent1.Genotype, parent2.Genotype, Encoding, random);
      genotypePopulation[i / 2] = child;
      crossoverCount++;
    }
    var endCrossover = Stopwatch.GetTimestamp();
    
    var startMutation = Stopwatch.GetTimestamp();
    int mutationCount = 0;
    for (int i = 0; i < genotypePopulation.Length; i++) {
      if (random.Random() < MutationRate) {
        genotypePopulation[i] = Mutator.Mutate(genotypePopulation[i], Encoding, random);
        mutationCount++;
      }
    }
    var endMutation = Stopwatch.GetTimestamp();
    
    var startDecoding = Stopwatch.GetTimestamp();
    var phenotypePopulation = genotypePopulation.Select(genotype => Decoder.Decode(genotype)).ToArray();
    var endDecoding = Stopwatch.GetTimestamp();
    
    var startEvaluation = Stopwatch.GetTimestamp();
    var fitnesses = phenotypePopulation.Select(phenotype => Evaluator.Evaluate(phenotype)).ToArray();
    var endEvaluation = Stopwatch.GetTimestamp();
    
    var population = Population.From(genotypePopulation, phenotypePopulation, fitnesses);
    
    var startReplacement = Stopwatch.GetTimestamp();
    var newPopulation = Replacer.Replace(oldPopulation, population, Objective, random);
    var endReplacement = Stopwatch.GetTimestamp();
    
    var endBeforeInterceptor = Stopwatch.GetTimestamp();

    var result = new EvolutionResult<TGenotype, TPhenotype>() {
      Generation = continuationState.Generation,
      Objective = Objective,
      Population = newPopulation,
      SelectionCount = parents.Count,
      CrossoverCount = crossoverCount,
      MutationCount = mutationCount,
      DecodingCount = phenotypePopulation.Length,
      EvaluationCount = fitnesses.Length,
      SelectionDuration = Stopwatch.GetElapsedTime(startSelection, endSelection),
      CrossoverDuration = Stopwatch.GetElapsedTime(startCrossover, endCrossover),
      MutationDuration = Stopwatch.GetElapsedTime(startMutation, endMutation),
      DecodingDuration = Stopwatch.GetElapsedTime(startDecoding, endDecoding),
      EvaluationDuration = Stopwatch.GetElapsedTime(startEvaluation, endEvaluation),
      ReplacementDuration = Stopwatch.GetElapsedTime(startReplacement, endReplacement),
      TotalDuration = Stopwatch.GetElapsedTime(start, endBeforeInterceptor)
    };
    
    var interceptorStart = Stopwatch.GetTimestamp();
    var interceptedResult = Interceptor.Transform(result);
    var interceptorEnd = Stopwatch.GetTimestamp();
    
    var end = Stopwatch.GetTimestamp();
    
    return interceptedResult with {
      TotalDuration = Stopwatch.GetElapsedTime(start, end),
      InterceptionDuration = Stopwatch.GetElapsedTime(interceptorStart, interceptorEnd)
    };
  }
  
  
  
  
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
