namespace HEAL.HeuristicLib.Optimization;

public readonly record struct ObjectiveValue(double Value)
{
    public static implicit operator ObjectiveValue(double value) => new(value);

    public int CompareTo(ObjectiveValue other, ObjectiveDirection objectiveDirection) => objectiveDirection switch
    {
        ObjectiveDirection.Minimize => Value.CompareTo(other.Value),
        ObjectiveDirection.Maximize => other.Value.CompareTo(Value),
        _ => throw new InvalidOperationException($"Unsupported objective direction: {objectiveDirection}.")
    };

    public bool IsBetterThan(ObjectiveValue other, ObjectiveDirection objectiveDirection) => CompareTo(other, objectiveDirection) < 0;
    public bool IsWorseThan(ObjectiveValue other, ObjectiveDirection objectiveDirection) => CompareTo(other, objectiveDirection) > 0;
    public bool IsEqualTo(ObjectiveValue other, ObjectiveDirection objectiveDirection) => CompareTo(other, objectiveDirection) == 0;

    public override string ToString() => $"{Value}";

    public static ObjectiveValue BestValue(ObjectiveDirection objectiveDirection) => objectiveDirection switch
    {
        ObjectiveDirection.Minimize => new ObjectiveValue(double.MinValue),
        ObjectiveDirection.Maximize => new ObjectiveValue(double.MaxValue),
        _ => throw new InvalidOperationException($"Unsupported objective direction: {objectiveDirection}.")
    };

    public static ObjectiveValue WorstValue(ObjectiveDirection objectiveDirection) => objectiveDirection switch
    {
        ObjectiveDirection.Minimize => new ObjectiveValue(double.MaxValue),
        ObjectiveDirection.Maximize => new ObjectiveValue(double.MinValue),
        _ => throw new InvalidOperationException($"Unsupported objective direction: {objectiveDirection}.")
    };
}
