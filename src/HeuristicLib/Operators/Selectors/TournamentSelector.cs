using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Selectors;

public record TournamentSelector<TGenotype>
  : StatelessSelector<TGenotype>
{
    public int TournamentSize { get; init; }

    public TournamentSelector(int tournamentSize)
    {
        TournamentSize = tournamentSize;
    }

    public override IReadOnlyList<Solution<TGenotype>> Select(IReadOnlyList<Solution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random)
      => TournamentSelector.Select(population, objective, count, random, TournamentSize);
}

public static class TournamentSelector
{
    public static IReadOnlyList<Solution<TGenotype>> Select<TGenotype>(
      IReadOnlyList<Solution<TGenotype>> population,
      Objective objective,
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
