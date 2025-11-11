using System.Linq;
using HEAL.HeuristicLib.Operators.Selector;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using LanguageExt.ClassInstances;

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

  public override IReadOnlyList<Solution<TGenotype>> Select(IReadOnlyList<Solution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random) {
    var fronts = DominationCalculator.CalculateAllParetoFronts(population, objective, out var rank, dominateOnEqualities);
    var lookup = new Dictionary<ObjectiveVector, double>();
    var res = new Solution<TGenotype>[count];
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

        if (idxRank > bestRank) continue;
        if (!lookup.TryGetValue(population[bestIdx].ObjectiveVector, out var dist)) {
          var frontObjectives = fronts[bestRank].Select(x => x.ObjectiveVector).ToArray();
          foreach (var (key, value) in frontObjectives.Zip(CrowdingDistance.CalculateCrowdingDistances(frontObjectives)))
            lookup[key] = value;
          dist = lookup[population[bestIdx].ObjectiveVector];
        }

        var dist2 = lookup[population[idx].ObjectiveVector];
        if (dist >= dist2) continue;
        bestIdx = idx;
        bestRank = rank[bestIdx];
      }

      res[i] = population[bestIdx];
    }

    return res;
  }
}

public class ParetoCrowdingReplacer<TGenotype>(bool dominateOnEqualities) : Replacer<TGenotype> {
  public override IReadOnlyList<Solution<TGenotype>> Replace(IReadOnlyList<Solution<TGenotype>> previousPopulation, IReadOnlyList<Solution<TGenotype>> offspringPopulation, Objective objective, IRandomNumberGenerator random) {
    var all = previousPopulation.Concat(offspringPopulation).ToArray();
    var fronts = DominationCalculator.CalculateAllParetoFronts(all, objective, out var rank, dominateOnEqualities);

    var l = new List<Solution<TGenotype>>();
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
