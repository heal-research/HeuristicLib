using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Algorithms;

public record PopulationResult<TGenotype>(Population<TGenotype> Population) : IAlgorithmResult<TGenotype>;
