using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Algorithms.EvolutionStrategy;

public enum EvolutionStrategyType {
  Comma,
  Plus
}

public record EvolutionStrategyPopulationState : PopulationState<RealVector, Fitness, Goal> {
  public required double MutationStrength { get; init; }
}

public class EvolutionStrategy : AlgorithmBase<EvolutionStrategyPopulationState> {
  public EvolutionStrategy(
    RealVectorEncoding encoding,
    int populationSize,
    int children,
    EvolutionStrategyType strategy,
    ICreator<RealVector, RealVectorEncoding> creator,
    IMutator<RealVector, RealVectorEncoding> mutator,
    double initialMutationStrength,
    ICrossover<RealVector, RealVectorEncoding>? crossover, //int parentsPerChild,
    IEvaluator<RealVector, Fitness> evaluator,
    Goal goal,
    ITerminator<EvolutionStrategyPopulationState>? terminator,
    IRandomSource randomSource) {
    Encoding = encoding;
    PopulationSize = populationSize;
    Children = children;
    Strategy = strategy;
    Creator = creator;
    Mutator = mutator;
    InitialMutationStrength = initialMutationStrength;
    Crossover = crossover;
    Evaluator = evaluator;
    Goal = goal;
    Terminator = terminator;
    RandomSource = randomSource;
  }
  
  public RealVectorEncoding Encoding { get; }
  public int PopulationSize { get; }
  public int Children { get; }
  public EvolutionStrategyType Strategy { get; }
  public ICreator<RealVector, RealVectorEncoding> Creator { get; }
  public IMutator<RealVector, RealVectorEncoding> Mutator { get; }
  public double InitialMutationStrength { get; }
  public ICrossover<RealVector, RealVectorEncoding>? Crossover { get; }
  public IEvaluator<RealVector, Fitness> Evaluator { get; }
  public Goal Goal { get; }
  public ITerminator<EvolutionStrategyPopulationState>? Terminator { get; }
  public IRandomSource RandomSource { get; }

  public override ExecutionStream<EvolutionStrategyPopulationState> CreateExecutionStream(EvolutionStrategyPopulationState? initialState = null) {
    return new ExecutionStream<EvolutionStrategyPopulationState>(InternalCreateExecutionStream(initialState));
  }
  
  private IEnumerable<EvolutionStrategyPopulationState> InternalCreateExecutionStream(EvolutionStrategyPopulationState? initialState) {
    var context = new AlgorithmContext<RealVectorEncoding> { Encoding = Encoding, Random = RandomSource.CreateRandomNumberGenerator() };
    
    EvolutionStrategyPopulationState currentState;
    if (initialState is null) {
      var initialPopulation = InitializePopulation(context);
      var evaluatedInitialPopulation = EvaluatePopulation(initialPopulation);
      yield return currentState = new EvolutionStrategyPopulationState { Goal = Goal, MutationStrength = InitialMutationStrength, Population = evaluatedInitialPopulation }; 
    } else {
      currentState = initialState;
    }
    
    while (Terminator?.ShouldContinue(currentState) ?? true) {
      var (offspringPopulation, successfulOffspring) = EvolvePopulation(currentState.Population, currentState.MutationStrength, context);
      var evaluatedOffspring = EvaluatePopulation(offspringPopulation);

      var newPopulation = Strategy switch {
        EvolutionStrategyType.Comma => evaluatedOffspring,
        EvolutionStrategyType.Plus => CombinePopulations(currentState.Population, evaluatedOffspring),
        _ => throw new NotImplementedException("Unknown strategy")
      };
      
      double successRate = (double)successfulOffspring / offspringPopulation.Length;
      double newMutationStrength = successRate switch {
        > 0.2 => currentState.MutationStrength * 1.5,
        < 0.2 => currentState.MutationStrength / 1.5,
        _ => currentState.MutationStrength
      };

      yield return currentState = currentState.Next() with { MutationStrength = newMutationStrength, Population = newPopulation };
    }
  }

  private RealVector[] InitializePopulation(AlgorithmContext<RealVectorEncoding> context) {
    var population = new RealVector[PopulationSize];
    for (int i = 0; i < PopulationSize; i++) {
      population[i] = Creator.Create(context);
    }
    return population;
  }

  private (RealVector[], int successfulOffspring) EvolvePopulation(Phenotype<RealVector, Fitness>[] population, double mutationStrength, AlgorithmContext<RealVectorEncoding> context) {
    var offspringPopulation = new RealVector[Children];
    for (int i = 0; i < Children; i++) {
      var parent = population[context.Random.Integer(PopulationSize)].Genotype;
      // var offspring = Mutator is IAdaptableMutator<RealVector> adaptableMutator 
      //   ? adaptableMutator.Mutate(parent, mutationStrength) 
      //   : Mutator.Mutate(parent);
      var offspring = Mutator.Mutate(parent, context);
      offspringPopulation[i] = offspring;
    }
    return (offspringPopulation, context.Random.Integer(Children, Children * 10));
    // actually calculate success rate
    // would require to evaluate individuals immediately or to store the parent for later comparison after child evaluation
  }

  private Phenotype<RealVector, Fitness>[] EvaluatePopulation(RealVector[] population) {
    return population.Select(individual => {
      var fitness = Evaluator.Evaluate(individual);
      return new Phenotype<RealVector, Fitness>(individual, fitness);
    }).ToArray();
  }

  private Phenotype<RealVector, Fitness>[] CombinePopulations(Phenotype<RealVector, Fitness>[] parents, Phenotype<RealVector, Fitness>[] offspring) {
    return parents.Concat(offspring)
      .Take(PopulationSize)
      .ToArray();
  }
}
