using HEAL.HeuristicLib.Algorithms.EvolutionStrategy;

namespace HEAL.HeuristicLib.Configuration;

public record EvolutionStrategySpec(
    int? PopulationSize = null,
    int? Children = null,
    EvolutionStrategyType? Strategy = null,
    CreatorSpec? Creator = null,
    CrossoverSpec? Crossover = null,
    MutatorSpec? Mutator = null,
    double? InitialMutationStrength = null,
    SelectorSpec? Selector = null,
    int? RandomSeed = null,
    int? MaximumGenerations = null
); 
