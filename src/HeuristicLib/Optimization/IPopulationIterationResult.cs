using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Optimization;

public interface IPopulationIterationResult<TGenotype, out TSelf> : IIterationResult
  where TSelf : IPopulationIterationResult<TGenotype, TSelf> {
  Population<TGenotype> Solutions { get; }
  TSelf WithISolutions(Population<TGenotype> solutions);
}
