using HEAL.HeuristicLib.Algorithms.EvolutionStrategy;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Configuration;

public record EvolutionStrategyConfiguration(
    int? PopulationSize = null,
    int? Children = null,
    EvolutionStrategyType? Strategy = null,
    ICreator<RealVector, RealVectorEncoding>? Creator = null,
    ICrossover<RealVector, RealVectorEncoding>? Crossover = null,
    IMutator<RealVector, RealVectorEncoding>? Mutator = null,
    double? InitialMutationStrength = null,
    ISelector? Selector = null
    //int? RandomSeed = null,
    //int? MaximumGenerations = null
); 
