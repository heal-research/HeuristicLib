using System.Collections;
using System.Globalization;

namespace HEAL.HeuristicLib.Optimization;

public sealed class ObjectiveVector : IReadOnlyList<double>, IEquatable<ObjectiveVector>
{
  private readonly double[] values;

  public ObjectiveVector(params double[] values)
    : this(values.AsSpan())
  {
  }

  public ObjectiveVector(params IReadOnlyList<double> values)
  {
    if (values.Count == 0) {
      throw new ArgumentException("Fitness vector must not be empty");
    }

    this.values = values.ToArray();
  }

  public ObjectiveVector(params ReadOnlySpan<double> values)
  {
    if (values.Length == 0) {
      throw new ArgumentException("Fitness vector must not be empty");
    }

    this.values = values.ToArray();
  }

  public ObjectiveVector(IEnumerable<double> values)
  {
    this.values = values.ToArray();
    if (this.values.Length == 0) {
      throw new ArgumentException("Fitness vector must not be empty");
    }
  }

  //public static implicit operator Fitness(SingleFitness[] values) => new(values);
  public static implicit operator ObjectiveVector(double[] values) => new(values /*.Select(v => new SingleFitness(v))*/);

  //public static implicit operator Fitness(SingleFitness value) => new(value);
  public static implicit operator ObjectiveVector(double value) => new(value);

  public bool IsSingleObjective => Count == 1;
  public ObjectiveValue? SingleFitness => values.SingleOrDefault();

  // public IEnumerator<SingleFitness> GetEnumerator() => ((IEnumerable<SingleFitness>)values).GetEnumerator();
  // IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
  // public int Count => values.Length;
  // public SingleFitness this[int index] => values[index];
  public IEnumerator<double> GetEnumerator() => ((IEnumerable<double>)values).GetEnumerator();
  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
  public int Count => values.Length;
  public double this[int index] => values[index];

  public bool Equals(ObjectiveVector? other)
  {
    if (other is null) {
      return false;
    }

    if (ReferenceEquals(this, other)) {
      return true;
    }

    if (Count != other.Count) {
      return false;
    }

    return values.SequenceEqual(other.values);
  }

  public override bool Equals(object? obj) => Equals(obj as ObjectiveVector);
  public override int GetHashCode() => values.Aggregate(0, HashCode.Combine);

  public DominanceRelation CompareTo(ObjectiveVector? other, Objective objective)
  {
    if (ReferenceEquals(this, other)) {
      return 0;
    }

    ArgumentNullException.ThrowIfNull(other);
    if (Count != other.Count) {
      throw new ArgumentException("Fitness vectors must have the same length");
    }

    if (Count != objective.Directions.Length) {
      throw new ArgumentException("Fitness vectors and directions must have the same length");
    }

    var comparisons = new int[Count];
    for (var i = 0; i < Count; i++) {
      comparisons[i] = this[i].CompareTo(other[i]);
      comparisons[i] *= objective.Directions[i] switch { ObjectiveDirection.Minimize => +1, ObjectiveDirection.Maximize => -1, _ => throw new NotImplementedException() };
    }

    var thisNotWorse = comparisons.All(c => c <= 0);
    var otherNotWorse = comparisons.All(c => c >= 0);

    return (thisNotWorse, otherNotWorse) switch {
      (true, true) => DominanceRelation.Equivalent,
      (true, false) => DominanceRelation.Dominates,
      (false, true) => DominanceRelation.IsDominatedBy,
      _ => DominanceRelation.Incomparable
    };
  }

  public bool Dominates(ObjectiveVector other, Objective goals) => CompareTo(other, goals) == DominanceRelation.Dominates;
  public bool IsDominatedBy(ObjectiveVector other, Objective goals) => CompareTo(other, goals) == DominanceRelation.IsDominatedBy;
  public bool IsEquivalentTo(ObjectiveVector other, Objective goals) => CompareTo(other, goals) == DominanceRelation.Equivalent;
  public bool IsIncomparableTo(ObjectiveVector other, Objective goals) => CompareTo(other, goals) == DominanceRelation.Incomparable;

  public override string ToString() => $"[{string.Join(", ", values.Select(v => v.ToString(CultureInfo.InvariantCulture)))}]";
}
