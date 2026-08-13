using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Replacers;

public record ElitismReplacer<TCandidate>
  : StatelessReplacer<TCandidate>
{
    public int Elites { get; init; }

    public ElitismReplacer(int elites)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(elites);
        Elites = elites;
    }

    public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace(IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation, IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random)
    {
        return ElitismReplacer.Replace(previousPopulation, offspringPopulation, objective, count, Elites);
    }
}

public static class ElitismReplacer
{
    public static ElitismReplacer<TCandidate> For<TCandidate, TSearchSpace>(IProblem<TCandidate, TSearchSpace> problem, int elites)
        where TSearchSpace : class, ISearchSpace<TCandidate> => new(elites);

    public static IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace<TCandidate>(
        IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation,
        IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation,
        ObjectiveDirections objective,
        int count,
        int elites)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(elites);

        var elitesPopulation = previousPopulation.OrderBy(p => p.ObjectiveVector, objective.TotalOrderComparer).Take(elites);
        var remainingCount = count - Math.Min(previousPopulation.Count, elites);
        var nonElites = offspringPopulation.Take(remainingCount);

        return elitesPopulation.Concat(nonElites).ToArray();
    }
}
