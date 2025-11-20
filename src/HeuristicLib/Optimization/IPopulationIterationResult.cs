using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Operators;

public interface IPopulationIterationResult<TGenotype, out TSelf> : IIterationResult
  where TSelf : IPopulationIterationResult<TGenotype, TSelf> {
  Population<TGenotype> Solutions { get; }
  TSelf WithISolutions(Population<TGenotype> solutions);
}
