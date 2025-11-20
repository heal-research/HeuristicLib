using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Algorithms;

public record SingleSolutionIterationResult<T>(Solution<T> Solution) : PopulationIterationResult<T>(new Population<T>([Solution]));
