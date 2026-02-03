using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Operators.Replacers;

public static class CrowdingDistance
{
  public static double[] CalculateCrowdingDistances(IReadOnlyList<ObjectiveVector> population)
  {
    var n = population.Count;
    switch (n) {
      case 0:
        return [];
      case 1:
        return [double.PositiveInfinity];
      case <= 2:
        return [double.PositiveInfinity, double.PositiveInfinity];
    }

    var distances = new double[n];

    var m = population[0].Count;// number of objectives
    var indices = Enumerable.Range(0, n).ToArray();
    // Compute for each objective dimension
    for (var obj = 0; obj < m; obj++) {
      // Sort indices by objective value
      Array.Sort(indices, new IndexedComparer(population, obj));
      var minVal = population[indices[0]][obj];
      var maxVal = population[indices[^1]][obj];
      var range = maxVal - minVal;

      if (range == 0.0) {
        continue;// avoid division by zero
      }

      // Boundary points get infinite distance
      distances[indices[0]] = double.PositiveInfinity;
      distances[indices[^1]] = double.PositiveInfinity;

      // Internal points
      for (var j = 1; j < n - 1; j++) {
        var prev = population[indices[j - 1]][obj];
        var next = population[indices[j + 1]][obj];

        // Normalize difference and add to crowding distance
        distances[indices[j]] += (next - prev) / range;
      }
    }

    return distances;
  }
}
