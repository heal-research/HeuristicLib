using HEAL.HeuristicLib.Algorithms;

namespace HEAL.HeuristicLib.Optimization;

public interface IPopulationAlgorithmState<TGenotype, out TSelf> : IAlgorithmState
  where TSelf : IPopulationAlgorithmState<TGenotype, TSelf> {
  Population<TGenotype> Solutions { get; }
  TSelf WithISolutions(Population<TGenotype> solutions);
}
