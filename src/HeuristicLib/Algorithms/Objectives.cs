using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace HEAL.HeuristicLib.Algorithms;

public enum ObjectiveDirection {
  Minimize,
  Maximize
}

public readonly record struct ObjectiveValue(double Value, ObjectiveDirection Direction) : IComparable<ObjectiveValue> {
  public static implicit operator ObjectiveValue((double Value, ObjectiveDirection Direction) objective) {
    return new ObjectiveValue(objective.Value, objective.Direction);
  }
  
  public int CompareTo(ObjectiveValue other) => Direction switch {
    ObjectiveDirection.Minimize => Value.CompareTo(other.Value),
    ObjectiveDirection.Maximize => other.Value.CompareTo(Value),
    _ => throw new NotImplementedException()
  };

  public bool IsBetterThan(ObjectiveValue other) => CompareTo(other) < 0;
  public bool IsWorseThan(ObjectiveValue other) => CompareTo(other) > 0;
  public bool IsEqualTo(ObjectiveValue other) => CompareTo(other) == 0;

  public static bool operator <(ObjectiveValue left, ObjectiveValue right) => left.CompareTo(right) < 0;
  public static bool operator >(ObjectiveValue left, ObjectiveValue right) => left.CompareTo(right) > 0;
  public static bool operator <=(ObjectiveValue left, ObjectiveValue right) => left.CompareTo(right) <= 0;
  public static bool operator >=(ObjectiveValue left, ObjectiveValue right) => left.CompareTo(right) >= 0;
  
  public override string ToString() => $"{Value} ({Direction})";

  public static ObjectiveValue GetBest(ObjectiveDirection direction) => direction switch {
    ObjectiveDirection.Minimize => new ObjectiveValue(double.MinValue, ObjectiveDirection.Minimize),
    ObjectiveDirection.Maximize => new ObjectiveValue(double.MaxValue, ObjectiveDirection.Maximize),
    _ => throw new NotImplementedException()
  };
  public static ObjectiveValue GetWorst(ObjectiveDirection direction) => direction switch {
    ObjectiveDirection.Minimize => new ObjectiveValue(double.MaxValue, ObjectiveDirection.Minimize),
    ObjectiveDirection.Maximize => new ObjectiveValue(double.MinValue, ObjectiveDirection.Maximize),
    _ => throw new NotImplementedException()
  };
  
}

public enum ParetoDominance {
  Dominates,
  IsDominatedBy,
  Equivalent,
  Incomparable
}

public interface IParetoComparable<in T> {
  ParetoDominance CompareTo(T other);
}

public sealed class ObjectiveVector 
  : IReadOnlyList<ObjectiveValue>, IEquatable<ObjectiveVector>, IParetoComparable<ObjectiveVector> 
{
  private readonly ObjectiveValue[] values;
  
  public ObjectiveVector(params IEnumerable<ObjectiveValue> values) {
    this.values = values.ToArray();
  }
  public ObjectiveVector(IReadOnlyList<double> values, IReadOnlyList<ObjectiveDirection> directions) {
    if (values.Count != directions.Count) throw new ArgumentException("Values and directions must have the same length");
    this.values = values.Zip(directions, (v, d) => new ObjectiveValue(v, d)).ToArray();
  }
  public ObjectiveVector(params IEnumerable<(double Value, ObjectiveDirection Direction)> values) {
    this.values = values.Select(v => new ObjectiveValue(v.Value, v.Direction)).ToArray();
  }

  public static implicit operator ObjectiveVector(ObjectiveValue value) {
    return new ObjectiveVector(value);
  }

  public IEnumerator<ObjectiveValue> GetEnumerator() => ((IEnumerable<ObjectiveValue>)values).GetEnumerator();
  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
  public int Count => values.Length;
  public ObjectiveValue this[int index] => values[index];
  
  public bool Equals(ObjectiveVector? other) {
    if (other is null) return false;
    if (ReferenceEquals(this, other)) return true;
    if (Count != other.Count) return false;
    return values.SequenceEqual(other.values);
  }
  public override bool Equals(object? obj) => Equals(obj as ObjectiveVector);
  public override int GetHashCode() => values.Aggregate(0, (hash, val) => HashCode.Combine(hash, val.GetHashCode()));

  public ParetoDominance CompareTo(ObjectiveVector? other) {
    if (ReferenceEquals(this, other)) return 0;
    ArgumentNullException.ThrowIfNull(other);
    if (Count != other.Count) throw new ArgumentException("Objective vectors must have the same length");
    
    var comparisons = values.Zip(other.values, (v1, v2) => v1.CompareTo(v2)).ToList();
    bool thisNotWorse = comparisons.All(c => c <= 0);
    bool otherNotWorse = comparisons.All(c => c >= 0);

    return (thisNotWorse, otherNotWorse) switch {
      (true, true) => ParetoDominance.Equivalent,
      (true, false) => ParetoDominance.Dominates,
      (false, true) => ParetoDominance.IsDominatedBy,
      _ => ParetoDominance.Incomparable
    };
  }

  public bool Dominates(ObjectiveVector other) => CompareTo(other) == ParetoDominance.Dominates;
  public bool IsDominatedBy(ObjectiveVector other) => CompareTo(other) == ParetoDominance.IsDominatedBy;
  public bool IsEquivalentTo(ObjectiveVector other) => CompareTo(other) == ParetoDominance.Equivalent;
  public bool IsIncomparableTo(ObjectiveVector other) => CompareTo(other) == ParetoDominance.Incomparable;

  public override string ToString() => $"[{string.Join(", ", values.Select(v => v.ToString()))}]";
}
