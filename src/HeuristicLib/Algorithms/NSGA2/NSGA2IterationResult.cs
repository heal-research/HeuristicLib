using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Algorithms.NSGA2;

public record NSGA2IterationResult<TGenotype>(Population<TGenotype> Population) : PopulationIterationResult<TGenotype>(Population);
