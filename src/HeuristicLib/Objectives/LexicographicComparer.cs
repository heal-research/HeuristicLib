namespace HEAL.HeuristicLib.Objectives;

public class LexicographicComparer : IComparer<ObjectiveVector>
{
    private readonly ImmutableArray<ObjectiveDirection> objectives;
    private readonly ImmutableArray<int> order;

    /// <param name="objectives">The direction of each objective.</param>
    /// <param name="order">
    /// Every objective index exactly once, in comparison priority order. When omitted, uses ascending index order.
    /// </param>
    public LexicographicComparer(IReadOnlyList<ObjectiveDirection> objectives, IReadOnlyList<int>? order = null)
    {
        this.objectives = objectives.ToImmutableArray();
        this.order = order is null
            ? Enumerable.Range(0, this.objectives.Length).ToImmutableArray()
            : order.ToImmutableArray();

        if (this.order.Length != this.objectives.Length)
            throw new ArgumentException("The order must contain every objective index exactly once.", nameof(order));

        var seen = new bool[this.objectives.Length];
        foreach (var dimension in this.order)
        {
            if (dimension < 0 || dimension >= seen.Length || seen[dimension])
                throw new ArgumentException("The order must contain every objective index exactly once.", nameof(order));

            seen[dimension] = true;
        }
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
