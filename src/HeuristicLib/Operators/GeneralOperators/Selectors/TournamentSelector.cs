using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

public class TournamentSelector<TGenotype>(int tournamentSize) : BatchSelector<TGenotype> {
  public int TournamentSize { get; set; } = tournamentSize;

  public override IReadOnlyList<Solution<TGenotype>> Select(IReadOnlyList<Solution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random) {
    var selected = new Solution<TGenotype>[count];
    var tournamentParticipants = new List<Solution<TGenotype>>(TournamentSize);
    for (var i = 0; i < selected.Length; i++) {
      tournamentParticipants.Clear();
      var randoms = random.Integers(population.Count, TournamentSize);
      for (var j = 0; j < TournamentSize; j++) {
        var index = randoms[j];
        tournamentParticipants.Add(population[index]);
      }

      var bestParticipant = tournamentParticipants.MinBy(participant => participant.ObjectiveVector, objective.TotalOrderComparer)!;
      selected[i] = bestParticipant;
    }

    return selected;
  }
}
