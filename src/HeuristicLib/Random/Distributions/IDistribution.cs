namespace HEAL.HeuristicLib.Random;

public interface IDistribution<out T>
{
    T Sample(IRandomNumberGenerator random);
}
