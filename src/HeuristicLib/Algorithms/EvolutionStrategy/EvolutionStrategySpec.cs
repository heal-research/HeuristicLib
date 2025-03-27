using HEAL.HeuristicLib.Algorithms.EvolutionStrategy;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Configuration;

public record EvolutionStrategyConfiguration(
    int? PopulationSize = null,
    int? Children = null,
    EvolutionStrategyType? Strategy = null,
    ICreator<RealVector, RealVectorEncodingParameter>? Creator = null,
    ICrossover<RealVector, RealVectorEncodingParameter>? Crossover = null,
    IMutator<RealVector, RealVectorEncodingParameter>? Mutator = null,
    double? InitialMutationStrength = null,
    ISelector? Selector = null
    //int? RandomSeed = null,
    //int? MaximumGenerations = null
); 
