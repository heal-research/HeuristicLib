using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>
/// Returns one of the two parents unchanged, chosen at random for each parent group.
/// </summary>
public record RandomCrossover<TCandidate> : SingleCandidateCrossover<TCandidate>
{
    /// <summary>
    /// Probability of choosing the first parent, normally in <c>[0,1]</c>. The default of <c>0.5</c> picks either
    /// parent equally often.
    /// </summary>
    /// <remarks>
    /// The value is used as a threshold rather than a validated ratio: at most zero, and <c>NaN</c>, always takes the
    /// second parent, while at least one always takes the first.
    /// </remarks>
    public double Bias { get; init; } = 0.5;

    public override TCandidate CrossParents(Parents<TCandidate> parents, IRandomNumberGenerator random) => random.NextDouble() < Bias ? parents.Parent1 : parents.Parent2;
}

public static class RandomCrossover
{
    public static RandomCrossover<TCandidate> For<TCandidate, TSearchSpace>(IProblem<TCandidate, TSearchSpace> problem, double bias = 0.5)
        where TSearchSpace : class, ISearchSpace<TCandidate> => new() { Bias = bias };
}
