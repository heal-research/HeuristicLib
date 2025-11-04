using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Selector;

public class TournamentSelector<TGenotype>(int tournamentSize) : BatchSelector<TGenotype> {
  public override IReadOnlyList<Solution<TGenotype>> Select(IReadOnlyList<Solution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random) {
    return Enumerable
           .Range(0, count)
           .Select(_ => random.Integers(tournamentSize, population.Count)
                              .Select(i1 => population[i1])
                              .MinBy(participant => participant.ObjectiveVector, objective.TotalOrderComparer)!)
           .ToArray();
  }
}
