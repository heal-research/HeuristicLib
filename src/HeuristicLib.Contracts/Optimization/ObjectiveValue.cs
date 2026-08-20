namespace HEAL.HeuristicLib.Optimization;

public readonly record struct ObjectiveValue(double Value)
{
    public static implicit operator ObjectiveValue(double value) => new(value);

    /// <summary>
    /// Orders two objective values by quality, so a negative result means <paramref name="left"/> is the better value.
    /// </summary>
    /// <remarks>
    /// <see cref="double.NaN"/> is the worst value in both objective directions, and two NaN values compare equal. The
    /// infinities are ordinary values and swap roles with the direction, so negative infinity is the best value of a
    /// minimized objective and the worst of a maximized one.
    /// </remarks>
    public static int Compare(double left, double right, ObjectiveDirection objectiveDirection)
    {
        var direction = objectiveDirection switch
        {
            ObjectiveDirection.Minimize => +1,
            ObjectiveDirection.Maximize => -1,
            _ => throw new InvalidOperationException($"Unsupported objective direction: {objectiveDirection}.")
        };

        return (double.IsNaN(left), double.IsNaN(right)) switch
        {
            (true, true) => 0,
            (true, false) => +1,
            (false, true) => -1,
            (false, false) => direction * left.CompareTo(right)
        };
    }

    /// <inheritdoc cref="Compare(double, double, ObjectiveDirection)"/>
    public int CompareTo(ObjectiveValue other, ObjectiveDirection objectiveDirection) =>
        Compare(Value, other.Value, objectiveDirection);

    public bool IsBetterThan(ObjectiveValue other, ObjectiveDirection objectiveDirection) => CompareTo(other, objectiveDirection) < 0;
    public bool IsWorseThan(ObjectiveValue other, ObjectiveDirection objectiveDirection) => CompareTo(other, objectiveDirection) > 0;
    public bool IsEqualTo(ObjectiveValue other, ObjectiveDirection objectiveDirection) => CompareTo(other, objectiveDirection) == 0;

    public override string ToString() => $"{Value}";

    /// <summary>
    /// The best value the direction admits: negative infinity when minimizing and positive infinity when maximizing.
    /// No objective value can be better, so it is a safe seed when accumulating a best-so-far value.
    /// </summary>
    public static ObjectiveValue BestValue(ObjectiveDirection objectiveDirection) => objectiveDirection switch
    {
        ObjectiveDirection.Minimize => new ObjectiveValue(double.NegativeInfinity),
        ObjectiveDirection.Maximize => new ObjectiveValue(double.PositiveInfinity),
        _ => throw new InvalidOperationException($"Unsupported objective direction: {objectiveDirection}.")
    };

    /// <summary>
    /// The worst value the direction admits: positive infinity when minimizing and negative infinity when maximizing.
    /// It is a safe seed when accumulating a worst-so-far value.
    /// </summary>
    /// <remarks>
    /// Only <see cref="double.NaN"/> ranks below this value.
    /// </remarks>
    public static ObjectiveValue WorstValue(ObjectiveDirection objectiveDirection) => objectiveDirection switch
    {
        ObjectiveDirection.Minimize => new ObjectiveValue(double.PositiveInfinity),
        ObjectiveDirection.Maximize => new ObjectiveValue(double.NegativeInfinity),
        _ => throw new InvalidOperationException($"Unsupported objective direction: {objectiveDirection}.")
    };
}
