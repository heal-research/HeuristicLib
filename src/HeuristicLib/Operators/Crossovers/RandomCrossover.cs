using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

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

    public override TCandidate Cross(IParents<TCandidate> parents, IRandomNumberGenerator random)
    {
        return random.NextDouble() < Bias ? parents.Parent1 : parents.Parent2;
    }
}
