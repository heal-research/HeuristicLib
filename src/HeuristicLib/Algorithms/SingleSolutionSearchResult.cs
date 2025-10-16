using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Algorithms;

public record SingleSolutionSearchResult<T>(Solution<T> Solution) : PopulationResult<T>(new Population<T>([Solution]));
