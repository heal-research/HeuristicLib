using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Algorithms;

public record SingleSolutionResult<T>(Solution<T> Solution) : PopulationResult<T>(new Population<T>([Solution]));
