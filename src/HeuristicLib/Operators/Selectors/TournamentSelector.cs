using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Selectors;

public record TournamentSelector<TCandidate>
  : StatelessSelector<TCandidate>
{
    public int TournamentSize { get; init; }

    public TournamentSelector(int tournamentSize)
    {
        TournamentSize = tournamentSize;
    }

    public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random)
      => TournamentSelector.Select(population, objective, count, random, TournamentSize);
}

public static class TournamentSelector
{
    public static IReadOnlyList<EvaluatedCandidate<TCandidate>> Select<TCandidate>(
      IReadOnlyList<EvaluatedCandidate<TCandidate>> population,
      ObjectiveDirections objective,
      int count,
      IRandomNumberGenerator random,
      int tournamentSize)
    {
        return Enumerable
               .Range(0, count)
               .Select(_ => random.NextInts(tournamentSize, population.Count)
                                  .Select(i1 => population[i1])
                                  .MinBy(participant => participant.ObjectiveVector, objective.TotalOrderComparer)!)
               .ToArray();
    }
}
