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
    IEvaluator<RealVector, ObjectiveValue> evaluator,
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
  public IEvaluator<RealVector, ObjectiveValue> Evaluator { get; }
  public ITerminator<EvolutionStrategyPopulationState>? Terminator { get; }
  public IRandomSource RandomSource { get; }

  public override EvolutionStrategyPopulationState Execute(EvolutionStrategyPopulationState? initialState = null, ITerminator<EvolutionStrategyPopulationState>? terminator = null) {
    if (Terminator is null && terminator is null) throw new InvalidOperationException("At least one terminator must be provided.");
    return CreateExecutionStream(initialState, terminator).Last();
  }

  public override ExecutionStream<EvolutionStrategyPopulationState> CreateExecutionStream(EvolutionStrategyPopulationState? initialState = null, ITerminator<EvolutionStrategyPopulationState>? termination = null) {
    return new ExecutionStream<EvolutionStrategyPopulationState>(InternalCreateExecutionStream(initialState, termination));
  }
  
  private IEnumerable<EvolutionStrategyPopulationState> InternalCreateExecutionStream(EvolutionStrategyPopulationState? initialState, ITerminator<EvolutionStrategyPopulationState>? terminator) {
    var rng = RandomSource.CreateRandomNumberGenerator();
    
    var activeTerminator = terminator ?? Terminator;

    EvolutionStrategyPopulationState currentState;
    if (initialState is null) {
      var initialPopulation = InitializePopulation();
      var initialObjectives = EvaluatePopulation(initialPopulation);
      yield return currentState = new EvolutionStrategyPopulationState { Generation = 0, MutationStrength = InitialMutationStrength, Population = initialPopulation, Objectives = initialObjectives }; 
    } else {
      currentState = initialState;
    }
    
    while (activeTerminator?.ShouldContinue(currentState) ?? true) {
      var (offspringPopulation, successfulOffspring) = EvolvePopulation(currentState.Population, currentState.MutationStrength, rng);
      var offspringObjectives = EvaluatePopulation(offspringPopulation);

      var (newPopulation, newObjectives) = Strategy switch {
        EvolutionStrategyType.Comma => (offspringPopulation, offspringObjectives),
        EvolutionStrategyType.Plus => CombinePopulations(currentState.Population, currentState.Objectives, offspringPopulation, offspringObjectives),
        _ => throw new NotImplementedException("Unknown strategy")
      };
      
      double successRate = (double)successfulOffspring / offspringPopulation.Length;
      double newMutationStrength = successRate switch {
        > 0.2 => currentState.MutationStrength * 1.5,
        < 0.2 => currentState.MutationStrength / 1.5,
        _ => currentState.MutationStrength
      };

      yield return currentState = currentState with { Generation = currentState.Generation + 1, MutationStrength = newMutationStrength, Population = newPopulation, Objectives = newObjectives };
    }
  }

  private RealVector[] InitializePopulation() {
    var population = new RealVector[PopulationSize];
    for (int i = 0; i < PopulationSize; i++) {
      population[i] = Creator.Create();
    }
    return population;
  }

  private (RealVector[], int successfulOffspring) EvolvePopulation(RealVector[] population, double mutationStrength, IRandomNumberGenerator rng) {
    var offspringPopulation = new RealVector[Children];
    for (int i = 0; i < Children; i++) {
      var parent = population[rng.Integer(PopulationSize)];
      var offspring = Mutator is IAdaptableMutator<RealVector> adaptableMutator 
        ? adaptableMutator.Mutate(parent, mutationStrength) 
        : Mutator.Mutate(parent);
      offspringPopulation[i] = offspring;
    }
    return (offspringPopulation, rng.Integer(Children, Children * 10));
    // actually calculate success rate
    // would require to evaluate individuals immediately or to store the parent for later comparison after child evaluation
  }

  private ObjectiveValue[] EvaluatePopulation(RealVector[] population) {
    return population.Select(individual => Evaluator.Evaluate(individual)).ToArray();
  }

  private (RealVector[], ObjectiveValue[]) CombinePopulations(RealVector[] parents, ObjectiveValue[] parentObjectives, RealVector[] offspring, ObjectiveValue[] offspringObjectives) {
    var combinedPopulation = parents.Concat(offspring).ToArray();
    var combinedObjectives = parentObjectives.Concat(offspringObjectives).ToArray();
    var sortedIndices = combinedObjectives
      .Select((objective, index) => new { objective, index })
      .OrderBy(x => x.objective)
      .Take(PopulationSize)
      .Select(x => x.index)
      .ToArray();

    var newPopulation = sortedIndices.Select(index => combinedPopulation[index]).ToArray();
    var newObjectives = sortedIndices.Select(index => combinedObjectives[index]).ToArray();
    return (newPopulation, newObjectives);
  }
}
