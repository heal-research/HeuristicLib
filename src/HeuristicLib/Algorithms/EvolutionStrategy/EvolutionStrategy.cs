using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Algorithms.EvolutionStrategy;

public enum EvolutionStrategyType {
  Comma,
  Plus
}

public record EvolutionStrategyPopulationState : PopulationState<RealVector> {
  public required double MutationStrength { get; init; }
}

public class EvolutionStrategy : AlgorithmBase<EvolutionStrategyPopulationState> {
  public EvolutionStrategy(
    int populationSize,
    int children,
    EvolutionStrategyType strategy,
    ICreator<RealVector> creator,
    IMutator<RealVector> mutator,
    double initialMutationStrength,
    ICrossover<RealVector>? crossover, //int parentsPerChild,
    IEvaluator<RealVector> evaluator,
    Objective objective,
    ITerminator<EvolutionStrategyPopulationState>? terminator,
    IRandomSource randomSource) {
    PopulationSize = populationSize;
    Children = children;
    Strategy = strategy;
    Creator = creator;
    Mutator = mutator;
    InitialMutationStrength = initialMutationStrength;
    Crossover = crossover;
    Evaluator = evaluator;
    Objective = objective;
    Terminator = terminator;
    RandomSource = randomSource;
  }
  
  public int PopulationSize { get; }
  public int Children { get; }
  public EvolutionStrategyType Strategy { get; }
  public ICreator<RealVector> Creator { get; }
  public IMutator<RealVector> Mutator { get; }
  public double InitialMutationStrength { get; }
  public ICrossover<RealVector>? Crossover { get; }
  public IEvaluator<RealVector> Evaluator { get; }
  public Objective Objective { get; }
  public ITerminator<EvolutionStrategyPopulationState>? Terminator { get; }
  public IRandomSource RandomSource { get; }

  public override ExecutionStream<EvolutionStrategyPopulationState> CreateExecutionStream(EvolutionStrategyPopulationState? initialState = null) {
    return new ExecutionStream<EvolutionStrategyPopulationState>(InternalCreateExecutionStream(initialState));
  }
  
  private IEnumerable<EvolutionStrategyPopulationState> InternalCreateExecutionStream(EvolutionStrategyPopulationState? initialState) {
    var random = RandomSource.CreateRandomNumberGenerator();
    
    EvolutionStrategyPopulationState currentState;
    if (initialState is null) {
      var initialPopulation = InitializePopulation(random);
      var evaluatedInitialPopulation = Evaluator.Evaluate(initialPopulation);
      yield return currentState = new EvolutionStrategyPopulationState { Objective = Objective, MutationStrength = InitialMutationStrength, Population = evaluatedInitialPopulation }; 
    } else {
      currentState = initialState;
    }
    
    while (Terminator?.ShouldContinue(currentState) ?? true) {
      var (offspringPopulation, successfulOffspring) = EvolvePopulation(currentState.Population, currentState.MutationStrength, random);
      var evaluatedOffspring = Evaluator.Evaluate(offspringPopulation);

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

  private RealVector[] InitializePopulation(IRandomNumberGenerator random) {
    var population = new RealVector[PopulationSize];
    for (int i = 0; i < PopulationSize; i++) {
      population[i] = Creator.Create(random);
    }
    return population;
  }

  private (RealVector[], int successfulOffspring) EvolvePopulation(Phenotype<RealVector>[] population, double mutationStrength, IRandomNumberGenerator random) {
    var offspringPopulation = new RealVector[Children];
    for (int i = 0; i < Children; i++) {
      var parent = population[random.Integer(PopulationSize)].Genotype;
      // var offspring = Mutator is IAdaptableMutator<RealVector> adaptableMutator 
      //   ? adaptableMutator.Mutate(parent, mutationStrength) 
      //   : Mutator.Mutate(parent);
      var offspring = Mutator.Mutate(parent, random);
      offspringPopulation[i] = offspring;
    }
    return (offspringPopulation, random.Integer(Children, Children * 10));
    // actually calculate success rate
    // would require to evaluate individuals immediately or to store the parent for later comparison after child evaluation
  }
  
  private Phenotype<RealVector>[] CombinePopulations(Phenotype<RealVector>[] parents, Phenotype<RealVector>[] offspring) {
    return parents.Concat(offspring)
      .Take(PopulationSize)
      .ToArray();
  }
}
