namespace HEAL.HeuristicLib.Optimization;

public sealed class ObjectiveDirections
{
    public ImmutableArray<ObjectiveDirection> Directions { get; }

    public IComparer<ObjectiveVector> TotalOrderComparer { get; }
    public ObjectiveVector Worst { get; }
    public ObjectiveVector Best { get; }

    public ObjectiveDirections(IReadOnlyList<ObjectiveDirection> directions, IComparer<ObjectiveVector> totalOrderComparer)
    {
        if (directions.Count == 0)
            throw new ArgumentException("Direction vector must not be empty");

        Directions = directions.ToImmutableArray();
        TotalOrderComparer = totalOrderComparer;
        Worst = new ObjectiveVector(Directions.Select(d => d == ObjectiveDirection.Minimize ? double.PositiveInfinity : double.NegativeInfinity));
        Best = new ObjectiveVector(Directions.Select(d => d == ObjectiveDirection.Maximize ? double.PositiveInfinity : double.NegativeInfinity));
    }

    public override string ToString() => $"[{string.Join(", ", Directions.Select(d => d.ToString()))}]";
}

public static class ObjectiveExtensions
{
    extension(IEnumerable<ObjectiveVector> values)
    {
        public ObjectiveVector Best(ObjectiveDirections o) =>
            values.Min(o.TotalOrderComparer) ?? throw new InvalidOperationException("Sequence contains no elements.");

        public ObjectiveVector Worst(ObjectiveDirections o) =>
            values.Max(o.TotalOrderComparer) ?? throw new InvalidOperationException("Sequence contains no elements.");

        public ObjectiveVector Median(ObjectiveDirections o)
        {
            var arr = values.ToArray();
            if (arr.Length == 0)
                throw new InvalidOperationException("Sequence contains no elements.");

            return arr.Order(o.TotalOrderComparer).ElementAt(arr.Length / 2);
        }
    }
}
