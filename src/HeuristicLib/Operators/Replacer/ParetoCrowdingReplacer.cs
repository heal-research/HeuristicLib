using HEAL.HeuristicLib.Operators.Selector;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Replacer;

public class CrowdingDistance {
  public static double[] CalculateCrowdingDistances(IReadOnlyList<ObjectiveVector> population) {
    var n = population.Count;
    switch (n) {
      case 0:
        return [];
      case 1:
        return [double.PositiveInfinity];
      case <= 2:
        return [double.PositiveInfinity, double.PositiveInfinity];
    }

    double[] distances = new double[n];

    var m = population[0].Count; // number of objectives
    var indices = Enumerable.Range(0, n).ToArray();
    // Compute for each objective dimension
    for (int obj = 0; obj < m; obj++) {
      // Sort indices by objective value
      Array.Sort(indices, new IndexedComparer(population, obj));
      var minVal = population[indices[0]][obj];
      var maxVal = population[indices[^1]][obj];
      var range = maxVal - minVal;

      if (range == 0.0)
        continue; // avoid division by zero

      // Boundary points get infinite distance
      distances[indices[0]] = double.PositiveInfinity;
      distances[indices[^1]] = double.PositiveInfinity;

      // Internal points
      for (int j = 1; j < n - 1; j++) {
        double prev = population[indices[j - 1]][obj];
        double next = population[indices[j + 1]][obj];

        // Normalize difference and add to crowding distance
        distances[indices[j]] += (next - prev) / range;
      }
    }

    return distances;
  }
}

public class ParetoCrowdingTournamentSelector<TGenotype> : BatchSelector<TGenotype> {
  private readonly int tournamentSize;
  private bool dominateOnEqualities;

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

public class ParetoCrowdingReplacer<TGenotype>(bool dominateOnEqualities) : Replacer<TGenotype> {
  public override IReadOnlyList<ISolution<TGenotype>> Replace(IReadOnlyList<ISolution<TGenotype>> previousPopulation, IReadOnlyList<ISolution<TGenotype>> offspringPopulation, Objective objective, IRandomNumberGenerator random) {
    var all = previousPopulation.Concat(offspringPopulation).ToArray();
    var fronts = DominationCalculator.CalculateAllParetoFronts(all, objective, out var rank, dominateOnEqualities);

    var l = new List<ISolution<TGenotype>>();
    var size = previousPopulation.Count;
    foreach (var front in fronts) {
      if (front.Count < size) {
        l.AddRange(front);
        size -= front.Count;
        continue;
      }

      var dist = CrowdingDistance.CalculateCrowdingDistances(front.Select(x => x.ObjectiveVector).ToList());
      l.AddRange(front.Select((x, i) => (x, i)).OrderBy(x => dist[x.i]).Select(x => x.x).Take(size));
      break;
    }

    return l;
  }

  public override int GetOffspringCount(int populationSize) => populationSize;
}
