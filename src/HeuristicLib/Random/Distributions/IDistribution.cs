namespace HEAL.HeuristicLib.Random.Distributions;

public interface IDistribution<out T>
{
    T Sample(IRandomNumberGenerator random);
}
