using HEAL.HeuristicLib.Genotypes.Vectors;

namespace HEAL.HeuristicLib.Optimization;

public class LexicographicComparer : IComparer<ObjectiveVector>
{
    private readonly ImmutableArray<ObjectiveDirection> objectives;
    private readonly Permutation order;

    public LexicographicComparer(IReadOnlyList<ObjectiveDirection> objectives, IReadOnlyList<int>? order = null)
    {
        this.objectives = objectives.ToImmutableArray();
        this.order = order is null
            ? Permutation.Range(objectives.Count)
            : Permutation.Create(order);
    }

    public int Compare(ObjectiveVector? x, ObjectiveVector? y)
    {
        if ((x is not null && x.Count != objectives.Length) || (y is not null && y.Count != objectives.Length))
            throw new ArgumentException("Objective vector must have the same length as the objective directions");
        if (x is null && y is null)
            return 0;
        if (x is null)
            return -1;
        if (y is null)
            return +1;

        foreach (var dimension in order)
        {
            var comparison = ObjectiveValue.Compare(x[dimension], y[dimension], objectives[dimension]);
            if (comparison != 0)
            {
                return comparison;
            }
        }

        return 0;
    }
}
