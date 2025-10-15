using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Algorithms.LocalSearch;

public record SingleSolutionIterationResult<T>(Solution<T> Solution) : IIterationResult<T>;
