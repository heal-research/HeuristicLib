namespace HEAL.HeuristicLib.Optimization;

public record SingleISolutionIterationResult<T>(ISolution<T> Solution) : PopulationIterationResult<T>(new Population<T>(Solution));
