using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Crossovers;

public record RandomCrossover<TCandidate>
  : SingleSolutionCrossover<TCandidate>
{
    public double Bias { get; }

    public RandomCrossover(double bias = 0.5)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(bias, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(bias, 1);

        Bias = bias;
    }

    public override TCandidate Cross(Parents<TCandidate> parents, IRandomNumberGenerator random)
    {
        return random.NextDouble() < Bias ? parents.Parent1 : parents.Parent2;
    }
}

public static class RandomCrossover
{
    public static RandomCrossover<TCandidate> For<TCandidate, TSearchSpace>(IProblem<TCandidate, TSearchSpace> problem, double bias = 0.5)
        where TSearchSpace : class, ISearchSpace<TCandidate> => new(bias);
}
