using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Replacers;

public sealed record ElitismReplacer<TCandidate>
    : StatelessReplacer<TCandidate>
{
    /// <summary>
    /// Gets the number of candidates requested from the previous population. The expected value is nonnegative.
    /// </summary>
    /// <remarks>
    /// A nonpositive value retains no previous candidates. A negative value correspondingly increases the number
    /// requested from the offspring population. The replacement never returns more than the requested count, so a
    /// value above that count is capped and leaves no places for offspring.
    /// </remarks>
    public int Elites { get; init; }

    public ElitismReplacer(int elites)
    {
        Elites = elites;
    }

    public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace(IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation, IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random) =>
        ElitismReplacer.Replace(previousPopulation, offspringPopulation, objective, count, Elites);
}

public static class ElitismReplacer
{
    public static ElitismReplacer<TCandidate> For<TCandidate, TSearchSpace>(IProblem<TCandidate, TSearchSpace> problem, int elites)
        where TSearchSpace : class, ISearchSpace<TCandidate> => new(elites);

    public static IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace<TCandidate>(IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation, IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation, ObjectiveDirections objective, int count, int elites)
    {
        var eliteCount = Math.Min(elites, count);
        var elitesPopulation = previousPopulation.OrderBy(p => p.ObjectiveVector, objective.TotalOrderComparer).Take(eliteCount);
        var remainingCount = count - Math.Min(previousPopulation.Count, eliteCount);
        var nonElites = offspringPopulation.Take(remainingCount);

        return elitesPopulation.Concat(nonElites).ToArray();
    }
}
