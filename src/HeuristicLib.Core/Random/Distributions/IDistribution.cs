namespace HEAL.HeuristicLib.Random.Distributions;

public interface IDistribution<out TResult> {
  TResult Sample(IRandomNumberGenerator rng);
}
