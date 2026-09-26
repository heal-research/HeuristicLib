using System.Collections;
using System.Globalization;

namespace HEAL.HeuristicLib.Objectives;

public sealed class ObjectiveVector : IReadOnlyList<double>, IEquatable<ObjectiveVector>
{
    private readonly double[] values;

    public ObjectiveVector(params IEnumerable<double> values)
    {
        this.values = values.ToArray();
    }

    public ObjectiveVector(params ReadOnlySpan<double> values)
    {
        this.values = values.ToArray();
    }

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

    /// <summary>
    /// Determines the Pareto relation of this vector to <paramref name="other"/> under the given objective directions.
    /// </summary>
    public DominanceRelation CompareTo(ObjectiveVector? other, ObjectiveDirections objective)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (Count != other.Count)
        {
            throw new ArgumentException("Objective values must have the same length");
        }

        if (Count != objective.Directions.Length)
        {
            throw new ArgumentException("Objective values and directions must have the same length");
        }

        // Skips the loop, which would reach the same result. Must stay below the argument checks.
        if (ReferenceEquals(this, other))
        {
            return DominanceRelation.Equal;
        }

        var thisNotWorse = true;
        var otherNotWorse = true;
        for (var i = 0; i < Count; i++)
        {
            var comparison = ObjectiveValue.Compare(this[i], other[i], objective.Directions[i]);
            if (comparison < 0)
            {
                otherNotWorse = false;
            }
            else if (comparison > 0)
            {
                thisNotWorse = false;
            }
        }

        return (thisNotWorse, otherNotWorse) switch
        {
            (true, true) => DominanceRelation.Equal,
            (true, false) => DominanceRelation.Dominates,
            (false, true) => DominanceRelation.IsDominatedBy,
            _ => DominanceRelation.Incomparable
        };
    }

    public bool Dominates(ObjectiveVector other, ObjectiveDirections objective) =>
        CompareTo(other, objective) == DominanceRelation.Dominates;

    public bool IsDominatedBy(ObjectiveVector other, ObjectiveDirections objective) =>
        CompareTo(other, objective) == DominanceRelation.IsDominatedBy;

    public bool IsEqualTo(ObjectiveVector other, ObjectiveDirections objective) =>
        CompareTo(other, objective) == DominanceRelation.Equal;

    public bool IsIncomparableTo(ObjectiveVector other, ObjectiveDirections objective) =>
        CompareTo(other, objective) == DominanceRelation.Incomparable;

    public override string ToString() =>
        $"[{string.Join(", ", values.Select(v => v.ToString(CultureInfo.InvariantCulture)))}]";

    public ObjectiveVector Add(ObjectiveVector apply)
    {
        return this.Zip(apply, (a, b) => a + b).ToArray();
    }
}
