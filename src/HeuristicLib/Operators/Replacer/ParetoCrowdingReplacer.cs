using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Replacer;

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

      var dist = CalculateCrowdingDistances(front.Select(x => x.ObjectiveVector).ToList());
      l.AddRange(front.Select((x, i) => (x, i)).OrderBy(x => dist[x.i]).Select(x => x.x).Take(size));
      break;
    }

    return l;
  }

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

  public override int GetOffspringCount(int populationSize) => populationSize;
}
