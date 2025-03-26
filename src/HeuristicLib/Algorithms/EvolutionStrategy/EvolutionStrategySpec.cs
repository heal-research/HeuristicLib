using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.EvolutionStrategy;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Configuration;

public record EvolutionStrategySpec(
    int? PopulationSize = null,
    int? Children = null,
    EvolutionStrategyType? Strategy = null,
    Creator? Creator = null,
    Crossover? Crossover = null,
    Mutator? Mutator = null,
    double? InitialMutationStrength = null,
    Selector? Selector = null,
    int? RandomSeed = null,
    int? MaximumGenerations = null
); 
