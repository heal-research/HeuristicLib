using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Replacers;

public class ParetoCrowdingTournamentSelector<TGenotype> : Selector<TGenotype> {
  private readonly int tournamentSize;
  private readonly bool dominateOnEqualities;

  public ParetoCrowdingTournamentSelector(bool dominateOnEqualities, int tournamentSize = 2) {
    this.dominateOnEqualities = dominateOnEqualities;
    this.tournamentSize = tournamentSize;
  }

  public override IReadOnlyList<ISolution<TGenotype>> Select(
    IReadOnlyList<ISolution<TGenotype>> population,
    Objective objective,
    int count,
    IRandomNumberGenerator random) {
    var fronts = DominationCalculator.CalculateAllParetoFronts(population, objective, out var rank, dominateOnEqualities);

    // Key by solution instead of ObjectiveVector
    var crowdingBySolution = new Dictionary<ISolution<TGenotype>, double>();
    var res = new ISolution<TGenotype>[count];

    var calculatedFront = new HashSet<int>();

    for (int i = 0; i < count; i++) {
      var bestIdx = random.Integer(population.Count);
      var bestRank = rank[bestIdx];

      for (int j = 1; j < tournamentSize; j++) {
        var idx = random.Integer(population.Count);
        var idxRank = rank[idx];
        if (idxRank < bestRank) {
          bestIdx = idx;
          bestRank = rank[bestIdx];
          continue;
        }

        if (idxRank > bestRank) continue; // worse rank

        // equal rank -> compare crowding

        // ensure we have distances for this front
        if (!calculatedFront.Contains(bestRank)) {
          var frontSolutions = fronts[bestRank];
          var frontObjectives = frontSolutions.Select(x => x.ObjectiveVector).ToArray();
          var distances = CrowdingDistance.CalculateCrowdingDistances(frontObjectives);
          for (int k = 0; k < frontSolutions.Count; k++)
            crowdingBySolution[frontSolutions[k]] = distances[k];
          calculatedFront.Add(bestRank);
        }

        if (crowdingBySolution[population[idx]] <= crowdingBySolution[population[bestIdx]])
          continue;

        bestIdx = idx;
        bestRank = idxRank;
      }

      res[i] = population[bestIdx];
    }

    return res;
  }
}
