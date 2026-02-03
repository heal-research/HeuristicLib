using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Selectors;

public class TournamentSelector<TGenotype>(int tournamentSize) : BatchSelector<TGenotype>
{
  public override IReadOnlyList<ISolution<TGenotype>> Select(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random)
  {
    return Enumerable
      .Range(0, count)
      .Select(_ => random.Integers(tournamentSize, population.Count)
        .Select(i1 => population[i1])
        .MinBy(keySelector: participant => participant.ObjectiveVector, objective.TotalOrderComparer)!)
      .ToArray();
  }
}
