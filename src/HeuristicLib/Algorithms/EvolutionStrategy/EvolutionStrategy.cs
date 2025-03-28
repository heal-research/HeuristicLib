using System.Diagnostics;
using HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;
using HEAL.HeuristicLib.Core;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Algorithms.EvolutionStrategy;

public enum EvolutionStrategyType {
  Comma,
  Plus
}

public record EsGenotypeStartPopulation : GenotypeStartPopulation<RealVector> {
  public required double MutationStrength { get; init; }
}
public record EsPhenotypeStartPopulation<TPhenotype> : PhenotypeStartPopulation<RealVector, TPhenotype> {
  public required double MutationStrength { get; init; }
}

// ToDo: proper inheritance would require CRTP
public record EsEvolutionResult<TPhenotype> : EvolutionResult, IContinuableResultState<EsPhenotypeStartPopulation<TPhenotype>> {
  public required Individual<RealVector, TPhenotype>[] Population { get; init; }
  public required double MutationStrength { get; init; }
  public EsPhenotypeStartPopulation<TPhenotype> GetNextContinuationState() => new() {
    Generation = Generation + 1,
    Population = Population,
    MutationStrength = MutationStrength
  };
}

public record class EvolutionStrategy<TPhenotype> 
  : IContinuableAlgorithm<EsGenotypeStartPopulation, EsPhenotypeStartPopulation<TPhenotype>, EsEvolutionResult<TPhenotype>> 
{
  public required RealVectorEncodingParameter EncodingParameter { get; init; }
  public required int PopulationSize { get; init; }
  //public required int Children { get; init; }
  public required EvolutionStrategyType Strategy { get; init; }
  public required ICreator<RealVector, RealVectorEncodingParameter> Creator { get; init; }
  public required IMutator<RealVector, RealVectorEncodingParameter> Mutator { get; init; }
  public required double InitialMutationStrength { get; init; }
  //public required ICrossover<RealVector, RealVectorEncodingParameter>? Crossover { get; init; }
  public required IDecoder<RealVector, TPhenotype> Decoder { get; init; }
  public required IEvaluator<TPhenotype> Evaluator { get; init; }
  public required Objective Objective { get; init; }
  //public ITerminator<EvolutionStrategyPopulationState>? Terminator { get; }
  public required IRandomSource RandomSource { get; init; }
  public IInterceptor<EsEvolutionResult<TPhenotype>> Interceptor { get; init; } = Interceptors.Identity<EsEvolutionResult<TPhenotype>>();
  
  // public EvolutionStrategy(
  //   RealVectorEncodingParameter encodingParameter,
  //   int populationSize,
  //   int children,
  //   EvolutionStrategyType strategy,
  //   ICreator<RealVector, RealVectorEncodingParameter> creator,
  //   IMutator<RealVector, RealVectorEncodingParameter> mutator,
  //   double initialMutationStrength,
  //   ICrossover<RealVector, RealVectorEncodingParameter>? crossover, //int parentsPerChild,
  //   IDecoder<RealVector, TPhenotype> decoder,
  //   IEvaluator<TPhenotype> evaluator,
  //   Objective objective,
  //   IInterceptor<EsEvolutionResult<TPhenotype>>? interceptor = null
  //   //ITerminator<EvolutionStrategyPopulationState>? terminator,
  //   //IRandomSource randomSource
  // ) {
  //   //EncodingParameter = encodingParameter;
  //   PopulationSize = populationSize;
  //   Children = children;
  //   Strategy = strategy;
  //   Creator = creator;
  //   Mutator = mutator;
  //   InitialMutationStrength = initialMutationStrength;
  //   Crossover = crossover;
  //   Evaluator = evaluator;
  //   Objective = objective;
  //   //Terminator = terminator;
  //   //RandomSource = randomSource;
  //   Interceptor = interceptor ?? Interceptors.Identity<EsEvolutionResult<TPhenotype>>();
  // }
  
  public virtual EsEvolutionResult<TPhenotype> Execute(EsGenotypeStartPopulation? startState = null) {
    var start = Stopwatch.GetTimestamp();

    var random = RandomSource.CreateRandomNumberGenerator();
    
    var givenPopulation = startState?.Population ?? [];
    int remainingCount = PopulationSize - givenPopulation.Length;

    var startCreating = Stopwatch.GetTimestamp();
    var newPopulation = Enumerable.Range(0, remainingCount).Select(i => Creator.Create(EncodingParameter, random)).ToArray();
    var endCreating = Stopwatch.GetTimestamp();
    
    var genotypePopulation = givenPopulation.Concat(newPopulation).Take(PopulationSize).ToArray();
    
    var startDecoding = Stopwatch.GetTimestamp();
    var phenotypePopulation = genotypePopulation.Select(genotype => Decoder.Decode(genotype)).ToArray();
    var endDecoding = Stopwatch.GetTimestamp();
    
    var startEvaluating = Stopwatch.GetTimestamp();
    var fitnesses = Evaluator.Evaluate(phenotypePopulation);
    var endEvaluating = Stopwatch.GetTimestamp();

    var population = Population.From(genotypePopulation, phenotypePopulation, fitnesses);

    var endBeforeInterceptor = Stopwatch.GetTimestamp();
    
    var result = new EsEvolutionResult<TPhenotype>() {
      Generation = startState?.Generation ?? 0,
      MutationStrength = InitialMutationStrength,
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
  public virtual EsEvolutionResult<TPhenotype> Execute(EsPhenotypeStartPopulation<TPhenotype> continuationState) {
    var start = Stopwatch.GetTimestamp();

    var random = RandomSource.CreateRandomNumberGenerator();
    
    var oldPopulation = continuationState.Population;
    
    var startSelection = Stopwatch.GetTimestamp();
    var randomSelector = new RandomSelector();
    var parents = randomSelector.Select(oldPopulation, Objective, PopulationSize, random).ToList();
    var endSelection = Stopwatch.GetTimestamp();

     
    var genotypePopulation = new RealVector[parents.Count];
    var startMutation = Stopwatch.GetTimestamp();
    for (int i = 0; i < parents.Count; i += 2) {
      var parent = parents[i];
      var child = Mutator.Mutate(parent.Genotype, EncodingParameter, random);
      genotypePopulation[i / 2] = child;
    }
    var endMutation = Stopwatch.GetTimestamp();
    
    // ToDo: optional crossover
    
    var startDecoding = Stopwatch.GetTimestamp();
    var phenotypePopulation = genotypePopulation.Select(genotype => Decoder.Decode(genotype)).ToArray();
    var endDecoding = Stopwatch.GetTimestamp();
    
    var startEvaluation = Stopwatch.GetTimestamp();
    var fitnesses = Evaluator.Evaluate(phenotypePopulation);
    var endEvaluation = Stopwatch.GetTimestamp();
    
    // timing the adaption check
    int successfulOffspring = 0;
    for (int i = 0; i < fitnesses.Length; i++) {
      if (fitnesses[i].CompareTo(parents[i].Fitness, Objective) == DominanceRelation.Dominates) {
        successfulOffspring++;
      }
    }
    double successRate = (double)successfulOffspring / genotypePopulation.Length;
    double newMutationStrength = successRate switch {
      > 0.2 => continuationState.MutationStrength * 1.5,
      < 0.2 => continuationState.MutationStrength / 1.5,
      _ => continuationState.MutationStrength
    };
    
    var population = Population.From(genotypePopulation, phenotypePopulation, fitnesses);
    
    var startReplacement = Stopwatch.GetTimestamp();
    IReplacer replacer = Strategy switch {
      EvolutionStrategyType.Comma => new ElitismReplacer(0),
      EvolutionStrategyType.Plus => new PlusSelectionReplacer(),
      _ => throw new InvalidOperationException($"Unknown strategy {Strategy}")
    };
    var newPopulation = replacer.Replace(oldPopulation, population, Objective, random);
    var endReplacement = Stopwatch.GetTimestamp();
    
    var endBeforeInterceptor = Stopwatch.GetTimestamp();

    var result = new EsEvolutionResult<TPhenotype>() {
      Generation = continuationState.Generation,
      MutationStrength = newMutationStrength,
      Objective = Objective,
      Population = newPopulation,
      SelectionCount = parents.Count,
      MutationCount = genotypePopulation.Length,
      DecodingCount = phenotypePopulation.Length,
      EvaluationCount = fitnesses.Length,
      SelectionDuration = Stopwatch.GetElapsedTime(startSelection, endSelection),
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
  
  
  // public override ResultStream<EvolutionStrategyPopulationState> CreateExecutionStream(EvolutionStrategyPopulationState? initialState = null) {
  //   return new ResultStream<EvolutionStrategyPopulationState>(InternalCreateExecutionStream(initialState));
  // }
  //
  // private IEnumerable<EvolutionStrategyPopulationState> InternalCreateExecutionStream(EvolutionStrategyPopulationState? initialState) {
  //   var random = RandomSource.CreateRandomNumberGenerator();
  //   
  //   EvolutionStrategyPopulationState currentState;
  //   if (initialState is null) {
  //     var initialPopulation = InitializePopulation();
  //     var evaluatedInitialPopulation = Evaluator.Evaluate(initialPopulation);
  //     yield return currentState = new EvolutionStrategyPopulationState { Objective = Objective, MutationStrength = InitialMutationStrength, Population = evaluatedInitialPopulation }; 
  //   } else {
  //     currentState = initialState;
  //   }
  //   
  //   while (Terminator?.ShouldContinue(currentState) ?? true) {
  //     var (offspringPopulation, successfulOffspring) = EvolvePopulation(currentState.Population, currentState.MutationStrength, random);
  //     var evaluatedOffspring = Evaluator.Evaluate(offspringPopulation);
  //
  //     var newPopulation = Strategy switch {
  //       EvolutionStrategyType.Comma => evaluatedOffspring,
  //       EvolutionStrategyType.Plus => CombinePopulations(currentState.Population, evaluatedOffspring),
  //       _ => throw new NotImplementedException("Unknown strategy")
  //     };
  //     
  //     double successRate = (double)successfulOffspring / offspringPopulation.Length;
  //     double newMutationStrength = successRate switch {
  //       > 0.2 => currentState.MutationStrength * 1.5,
  //       < 0.2 => currentState.MutationStrength / 1.5,
  //       _ => currentState.MutationStrength
  //     };
  //
  //     yield return currentState = currentState.Next() with { MutationStrength = newMutationStrength, Population = newPopulation };
  //   }
  // }
  //
  // private RealVector[] InitializePopulation() {
  //   var population = new RealVector[PopulationSize];
  //   for (int i = 0; i < PopulationSize; i++) {
  //     population[i] = Creator.Create();
  //   }
  //   return population;
  // }
  //
  // private (RealVector[], int successfulOffspring) EvolvePopulation(Solution<RealVector>[] population, double mutationStrength, IRandomNumberGenerator random) {
  //   var offspringPopulation = new RealVector[Children];
  //   for (int i = 0; i < Children; i++) {
  //     var parent = population[random.Integer(PopulationSize)].Phenotype;
  //     // var offspring = Mutator is IAdaptableMutator<RealVector> adaptableMutator 
  //     //   ? adaptableMutator.Mutate(parent, mutationStrength) 
  //     //   : Mutator.Mutate(parent);
  //     var offspring = Mutator.Mutate(parent);
  //     offspringPopulation[i] = offspring;
  //   }
  //   return (offspringPopulation, random.Integer(Children, Children * 10));
  //   // actually calculate success rate
  //   // would require to evaluate individuals immediately or to store the parent for later comparison after child evaluation
  // }
  //
  // private Solution<RealVector>[] CombinePopulations(Solution<RealVector>[] parents, Solution<RealVector>[] offspring) {
  //   return parents.Concat(offspring)
  //     .Take(PopulationSize)
  //     .ToArray();
  // }
}
