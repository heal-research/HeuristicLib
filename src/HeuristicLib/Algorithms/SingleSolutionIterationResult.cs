using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Algorithms;

public record SingleISolutionIterationResult<T>(ISolution<T> Solution) : PopulationIterationResult<T>(new Population<T>(Solution));
