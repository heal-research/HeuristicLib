using System.Diagnostics;
using HEAL.HeuristicLib.Core;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;

public class GeneticAlgorithm<TGenotype, TPhenotype, TEncodingParameter>
  : AlgorithmBase<
      GenotypeStartPopulation<TGenotype>, 
      PhenotypeStartPopulation<TGenotype, TPhenotype>,
      EvolutionResult<TGenotype, TPhenotype>
  >
  where TEncodingParameter : IEncodingParameter<TGenotype>
{
  public TEncodingParameter EncodingParameter { get; }
  public int PopulationSize { get; }
  public ICreator<TGenotype, TEncodingParameter> Creator { get; }
  public ICrossover<TGenotype, TEncodingParameter> Crossover { get; }
  public IMutator<TGenotype, TEncodingParameter> Mutator { get; }
  public double MutationRate { get; }
  public IEvaluator<TGenotype, TPhenotype> Evaluator { get; }
  public Objective Objective { get; }
  //public IRandomSource RandomSource { get; }
  public ISelector Selector { get; }
  public IReplacer Replacer { get; }
  //public ITerminator<PopulationState<TGenotype>>? Terminator { get; }
  public IInterceptor<EvolutionResult<TGenotype, TPhenotype>> Interceptor { get; }
  
  public GeneticAlgorithm(
    TEncodingParameter encodingParameter,
    int populationSize,
    ICreator<TGenotype, TEncodingParameter> creator,
    ICrossover<TGenotype, TEncodingParameter> crossover, 
    IMutator<TGenotype, TEncodingParameter> mutator, double mutationRate,
    IEvaluator<TGenotype, TPhenotype> evaluator, Objective objective,
    ISelector selector, IReplacer replacer,
    //IRandomSource randomSourceState,
    //ITerminator<PopulationState<TGenotype>>? terminator = null,
    IInterceptor<EvolutionResult<TGenotype, TPhenotype>>? interceptor = null)
  {
    EncodingParameter = encodingParameter;
    PopulationSize = populationSize;
    Creator = creator;
    Crossover = crossover;
    Mutator = mutator;
    MutationRate = mutationRate;
    Evaluator = evaluator;
    Objective = objective;
    Selector = selector;
    Replacer = replacer;
    //RandomSource = randomSourceState;
    //Terminator = terminator;
    Interceptor = interceptor ?? Interceptors.Identity<EvolutionResult<TGenotype, TPhenotype>>();
  }
  
  public override EvolutionResult<TGenotype, TPhenotype> Execute(IRandomNumberGenerator random, GenotypeStartPopulation<TGenotype>? startState = null) {
    var start = Stopwatch.GetTimestamp();
    
    var givenPopulation = startState?.Population ?? [];
    int remainingCount = PopulationSize - givenPopulation.Length;

    var startCreating = Stopwatch.GetTimestamp();
    var newPopulation = Enumerable.Range(0, remainingCount).Select(i => Creator.Create(EncodingParameter, random)).ToArray();
    var endCreating = Stopwatch.GetTimestamp();
    
    var population = givenPopulation.Concat(newPopulation).Take(PopulationSize).ToArray();
    
    var startEvaluating = Stopwatch.GetTimestamp();
    var evaluatedPopulation = Evaluator.Evaluate(population);
    var endEvaluating = Stopwatch.GetTimestamp();

    var endBeforeInterceptor = Stopwatch.GetTimestamp();
    
    var result = new EvolutionResult<TGenotype, TPhenotype>() {
      Generation = startState?.Generation ?? 0,
      Objective = Objective,
      Population = evaluatedPopulation,
      CreationCount = remainingCount,
      EvaluationCount = evaluatedPopulation.Length,
      CreationDuration = Stopwatch.GetElapsedTime(startCreating, endCreating),
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
  
  public override EvolutionResult<TGenotype, TPhenotype> Execute(IRandomNumberGenerator random, PhenotypeStartPopulation<TGenotype, TPhenotype> continuationState) {
    var start = Stopwatch.GetTimestamp();
    
    int offspringCount = Replacer.GetOffspringCount(PopulationSize);

    var oldPopulation = continuationState.Population;
    
    var startSelection = Stopwatch.GetTimestamp();
    var parents = Selector.Select(oldPopulation, Objective, offspringCount * 2, random).ToList();
    var endSelection = Stopwatch.GetTimestamp();

     
    var offspring = new TGenotype[offspringCount];
    var startCrossover = Stopwatch.GetTimestamp();
    int crossoverCount = 0;
    for (int i = 0; i < parents.Count; i += 2) {
      var parent1 = parents[i];
      var parent2 = parents[i + 1];
      var child = Crossover.Cross(parent1.Genotype, parent2.Genotype, EncodingParameter, random);
      offspring[i / 2] = child;
      crossoverCount++;
    }
    var endCrossover = Stopwatch.GetTimestamp();
    
    var startMutation = Stopwatch.GetTimestamp();
    int mutationCount = 0;
    for (int i = 0; i < offspring.Length; i++) {
      if (random.Random() < MutationRate) {
        offspring[i] = Mutator.Mutate(offspring[i], EncodingParameter, random);
        mutationCount++;
      }
    }
    var endMutation = Stopwatch.GetTimestamp();
    
    var startEvaluation = Stopwatch.GetTimestamp();
    var evaluatedOffspring = Evaluator.Evaluate(offspring);
    var endEvaluation = Stopwatch.GetTimestamp();
    
    var startReplacement = Stopwatch.GetTimestamp();
    var newPopulation = Replacer.Replace(oldPopulation, evaluatedOffspring, Objective, random);
    var endReplacement = Stopwatch.GetTimestamp();
    
    var endBeforeInterceptor = Stopwatch.GetTimestamp();

    var result = new EvolutionResult<TGenotype, TPhenotype>() {
      Generation = continuationState.Generation,
      Objective = Objective,
      Population = newPopulation,
      SelectionCount = parents.Count,
      CrossoverCount = crossoverCount,
      MutationCount = mutationCount,
      EvaluationCount = evaluatedOffspring.Length,
      SelectionDuration = Stopwatch.GetElapsedTime(startSelection, endSelection),
      CrossoverDuration = Stopwatch.GetElapsedTime(startCrossover, endCrossover),
      MutationDuration = Stopwatch.GetElapsedTime(startMutation, endMutation),
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
  //     var offspring = Crossover.Cross(parent1.Genotype, parent2.Genotype, EncodingParameter, random);
  //     if (random.Random() < MutationRate) {
  //       offspring = Mutator.Mutate(offspring, EncodingParameter, random);
  //     }
  //     newPopulation[i / 2] = offspring;
  //   }
  //   return newPopulation;
  // }

}
