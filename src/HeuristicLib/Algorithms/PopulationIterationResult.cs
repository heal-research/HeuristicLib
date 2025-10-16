using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Algorithms;

public record PopulationIterationResult<TGenotype>(Population<TGenotype> Population) : IIterationResult<TGenotype>;
