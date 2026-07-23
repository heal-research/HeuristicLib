using System.Collections;
using System.Globalization;

namespace HEAL.HeuristicLib.Optimization;

public sealed class ObjectiveVector : IReadOnlyList<double>, IEquatable<ObjectiveVector>
{
    private readonly double[] values;

    public ObjectiveVector(params IEnumerable<double> values) => this.values = values.ToArray();

    public ObjectiveVector(params ReadOnlySpan<double> values) => this.values = values.ToArray();

    public bool IsSingleObjective => Count == 1;
    public ObjectiveValue? SingleObjectiveValue => Count == 1 ? new ObjectiveValue(values[0]) : null;

    public IEnumerator<double> GetEnumerator() => ((IEnumerable<double>)values).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public int Count => values.Length;
    public double this[int index] => values[index];

    public static implicit operator ObjectiveVector(double[] values) => new(values);
    public static implicit operator ObjectiveVector(double value) => new(value);

    public bool Equals(ObjectiveVector? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (Count != other.Count)
        {
            return false;
        }

        return values.SequenceEqual(other.values);
    }

    public override bool Equals(object? obj) => Equals(obj as ObjectiveVector);
    public override int GetHashCode() => values.Aggregate(0, HashCode.Combine);

    public DominanceRelation CompareTo(ObjectiveVector? other, ObjectiveDirections objective)
    {
        if (ReferenceEquals(this, other))
        {
            return 0;
        }

        ArgumentNullException.ThrowIfNull(other);

        if (Count != other.Count)
        {
            throw new ArgumentException("Objective values must have the same length");
        }

        if (Count != objective.Directions.Length)
        {
            throw new ArgumentException("Objective values and directions must have the same length");
        }

        var comparisons = new int[Count];
        for (var i = 0; i < Count; i++)
        {
            comparisons[i] = this[i].CompareTo(other[i]);
            comparisons[i] *= objective.Directions[i] switch
            {
                ObjectiveDirection.Minimize => +1,
                ObjectiveDirection.Maximize => -1,
                _ => throw new NotImplementedException()
            };
        }

        var thisNotWorse = comparisons.All(c => c <= 0);
        var otherNotWorse = comparisons.All(c => c >= 0);

        return (thisNotWorse, otherNotWorse) switch
        {
            (true, true) => DominanceRelation.Equivalent,
            (true, false) => DominanceRelation.Dominates,
            (false, true) => DominanceRelation.IsDominatedBy,
            _ => DominanceRelation.Incomparable
        };
    }

    public bool Dominates(ObjectiveVector other, ObjectiveDirections objective) =>
        CompareTo(other, objective) == DominanceRelation.Dominates;

    public bool IsDominatedBy(ObjectiveVector other, ObjectiveDirections objective) =>
        CompareTo(other, objective) == DominanceRelation.IsDominatedBy;

    public bool IsEquivalentTo(ObjectiveVector other, ObjectiveDirections objective) =>
        CompareTo(other, objective) == DominanceRelation.Equivalent;

    public bool IsIncomparableTo(ObjectiveVector other, ObjectiveDirections objective) =>
        CompareTo(other, objective) == DominanceRelation.Incomparable;

    public override string ToString() =>
        $"[{string.Join(", ", values.Select(v => v.ToString(CultureInfo.InvariantCulture)))}]";

    public ObjectiveVector Add(ObjectiveVector apply)
    {
        return this.Zip(apply, (a, b) => a + b).ToArray();
    }
}
