using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Selectors;

public class TournamentSelector<TGenotype> 
  : StatelessSelector<TGenotype>
{
  private readonly int tournamentSize;
  public TournamentSelector(int tournamentSize) {
    this.tournamentSize = tournamentSize;
  }

  public override IReadOnlyList<ISolution<TGenotype>> Select(IReadOnlyList<ISolution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random)
  {
    return Enumerable
      .Range(0, count)
      .Select(_ => random.NextInts(tournamentSize, population.Count)
        .Select(i1 => population[i1])
        .MinBy(participant => participant.ObjectiveVector, objective.TotalOrderComparer)!)
      .ToArray();
  }
}
