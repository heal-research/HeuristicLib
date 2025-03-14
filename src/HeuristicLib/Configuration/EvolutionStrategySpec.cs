using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.EvolutionStrategy;
using HEAL.HeuristicLib.Encodings;

namespace HEAL.HeuristicLib.Configuration;

public record EvolutionStrategySpec(
    int? PopulationSize = null,
    int? Children = null,
    EvolutionStrategyType? Strategy = null,
    CreatorSpec<RealVector, RealVectorEncoding>? Creator = null,
    CrossoverSpec<RealVector, RealVectorEncoding>? Crossover = null,
    MutatorSpec<RealVector, RealVectorEncoding>? Mutator = null,
    double? InitialMutationStrength = null,
    SelectorSpec<RealVector, Fitness, Goal>? Selector = null,
    int? RandomSeed = null,
    int? MaximumGenerations = null
); 

