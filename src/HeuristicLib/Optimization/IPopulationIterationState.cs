using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Optimization;

public interface IPopulationIterationState<TGenotype, out TSelf> : IIterationState
  where TSelf : IPopulationIterationState<TGenotype, TSelf> {
  Population<TGenotype> Solutions { get; }
  TSelf WithISolutions(Population<TGenotype> solutions);
}
