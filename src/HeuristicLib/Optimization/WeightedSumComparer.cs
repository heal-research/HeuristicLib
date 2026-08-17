namespace HEAL.HeuristicLib.Optimization;

/// <summary>
/// Orders objective vectors by their weighted sum. A zero weight excludes its objective from the comparison.
/// </summary>
public class WeightedSumComparer : IComparer<ObjectiveVector>
{
    private readonly ImmutableArray<double> directedWeights;

    public WeightedSumComparer(IReadOnlyList<ObjectiveDirection> objectives, IReadOnlyList<double>? weights = null)
    {
        if (weights is not null && objectives.Count != weights.Count)
        {
            throw new ArgumentException("Objective and weights must have the same length");
        }

        var builder = ImmutableArray.CreateBuilder<double>(objectives.Count);
        for (var i = 0; i < objectives.Count; i++)
        {
            var direction = objectives[i] switch
            {
                ObjectiveDirection.Minimize => +1.0,
                ObjectiveDirection.Maximize => -1.0,
                _ => throw new InvalidOperationException($"Unsupported objective direction: {objectives[i]}.")
            };

            builder.Add((weights?[i] ?? 1.0) * direction);
        }

        directedWeights = builder.MoveToImmutable();
    }

    public int Compare(ObjectiveVector? x, ObjectiveVector? y)
    {
        if ((x is not null && x.Count != directedWeights.Length) || (y is not null && y.Count != directedWeights.Length))
        {
            throw new ArgumentException("Objective vector must have the same length as the objective directions");
        }

        if (x is null && y is null)
        {
            return 0;
        }

        if (x is null)
        {
            return -1;
        }

        if (y is null)
        {
            return +1;
        }

        // The directions are folded into the weights, so the lower directed sum is the better one.
        return ObjectiveValue.Compare(WeightedSum(x), WeightedSum(y), ObjectiveDirection.Minimize);
    }

    private double WeightedSum(ObjectiveVector objectiveVector)
    {
        var sum = 0.0;
        for (var i = 0; i < directedWeights.Length; i++)
        {
            var weight = directedWeights[i];

            // A zero weight excludes its objective, so it must contribute nothing even where the objective value is
            // infinite. Multiplying would yield NaN and rank the whole vector last.
            if (weight != 0.0)
            {
                sum += weight * objectiveVector[i];
            }
        }

        return sum;
    }
}
