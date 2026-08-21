namespace HEAL.HeuristicLib.Objectives;

public static class CrowdingDistance
{
    public static double[] CalculateCrowdingDistances(IReadOnlyList<ObjectiveVector> population)
    {
        var n = population.Count;
        switch (n)
        {
            case 0:
                return [];
            case 1:
                return [double.PositiveInfinity];
            case <= 2:
                return [double.PositiveInfinity, double.PositiveInfinity];
        }

        var distances = new double[n];

        var m = population[0].Count; // number of objectives
        var indices = Enumerable.Range(0, n).ToArray();
        // Compute for each objective dimension
        for (var obj = 0; obj < m; obj++)
        {
            // Sort indices by objective value
            Array.Sort(indices, new IndexedComparer(population, obj));

            // NaN sorts last and takes no part in this dimension. It is the worst possible value, so treating it as a
            // boundary point would award it the maximum crowding distance. Its distance stays at zero instead.
            var orderedCount = n;
            while (orderedCount > 0 && double.IsNaN(population[indices[orderedCount - 1]][obj]))
            {
                orderedCount--;
            }

            if (orderedCount < 2)
            {
                continue;
            }

            var minVal = population[indices[0]][obj];
            var maxVal = population[indices[orderedCount - 1]][obj];
            var range = maxVal - minVal;

            // A non-finite range cannot normalize a difference: every internal distance would collapse to zero or NaN.
            if (range <= 0.0 || !double.IsFinite(range))
            {
                continue; // avoid division by zero
            }

            // Boundary points get infinite distance
            distances[indices[0]] = double.PositiveInfinity;
            distances[indices[orderedCount - 1]] = double.PositiveInfinity;

            // Internal points
            for (var j = 1; j < orderedCount - 1; j++)
            {
                var prev = population[indices[j - 1]][obj];
                var next = population[indices[j + 1]][obj];

                // Normalize difference and add to crowding distance
                distances[indices[j]] += (next - prev) / range;
            }
        }

        return distances;
    }
}
