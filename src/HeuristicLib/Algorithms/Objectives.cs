using System.Collections;

namespace HEAL.HeuristicLib.Algorithms;

public enum ObjectiveType {
  Minimize,
  Maximize
}

public readonly record struct ObjectiveValue(double Value, ObjectiveType Type)
  : IComparable<ObjectiveValue> 
{
  public int CompareTo(ObjectiveValue other) => Type switch {
    ObjectiveType.Minimize => Value.CompareTo(other.Value),
    ObjectiveType.Maximize => other.Value.CompareTo(Value),
    _ => throw new NotImplementedException()
  };

  public static bool operator <(ObjectiveValue left, ObjectiveValue right) => left.CompareTo(right) < 0;
  public static bool operator >(ObjectiveValue left, ObjectiveValue right) => left.CompareTo(right) > 0;
  public static bool operator <=(ObjectiveValue left, ObjectiveValue right) => left.CompareTo(right) <= 0;
  public static bool operator >=(ObjectiveValue left, ObjectiveValue right) => left.CompareTo(right) >= 0;
  
}

public class ObjectiveVector 
  : IReadOnlyList<ObjectiveValue>, IEquatable<ObjectiveVector>, IComparable<ObjectiveVector> 
{
  private readonly ObjectiveValue[] values;
  
  public ObjectiveVector(params IReadOnlyList<ObjectiveValue> values) {
    this.values = values.ToArray();
  }
  public ObjectiveVector(IReadOnlyList<double> values, IReadOnlyList<ObjectiveType> types) {
    if (values.Count != types.Count) throw new ArgumentException("Values and types must have the same length");
    this.values = values.Zip(types, (v, t) => new ObjectiveValue(v, t)).ToArray();
  }
  public ObjectiveVector(params IReadOnlyList<(double Value, ObjectiveType Type)> values) {
    this.values = values.Select(v => new ObjectiveValue(v.Value, v.Type)).ToArray();
  }

  public IEnumerator<ObjectiveValue> GetEnumerator() => (IEnumerator<ObjectiveValue>)values.GetEnumerator();
  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
  public int Count => values.Length;
  public ObjectiveValue this[int index] => values[index];
  
  public bool Equals(ObjectiveVector? other) {
    if (other is null) return false;
    if (ReferenceEquals(this, other)) return true;
    if (Count != other.Count) return false;
    return values.SequenceEqual(other.values);
  }

  public override bool Equals(object? obj) => 
    Equals(obj as ObjectiveVector);

  public override int GetHashCode() => 
    values.Aggregate(0, (hash, val) => HashCode.Combine(hash, val.GetHashCode()));

  public int CompareTo(ObjectiveVector? other) {
    if (other is null) return 1;
    if (ReferenceEquals(this, other)) return 0;
    
    if (Count != other.Count) throw new ArgumentException("Objective vectors must have the same length");
    
    var comparisons = values.Zip(other.values, (v1, v2) => v1.CompareTo(v2)).ToList();
    bool thisNotWorse = comparisons.All(c => c <= 0);
    bool otherNotWorse = comparisons.All(c => c >= 0);

    return (thisNotWorse, otherNotWorse) switch {
      (true, true) => 0,   // equal
      (true, false) => -1, // this dominates
      (false, true) => 1,  // other dominates
      _ => 0               // incomparable
    };
  }

  public static bool operator <(ObjectiveVector left, ObjectiveVector right) => left.CompareTo(right) < 0;
  public static bool operator >(ObjectiveVector left, ObjectiveVector right) => left.CompareTo(right) > 0;
  public static bool operator <=(ObjectiveVector left, ObjectiveVector right) => left.CompareTo(right) <= 0;
  public static bool operator >=(ObjectiveVector left, ObjectiveVector right) => left.CompareTo(right) >= 0;
  public static bool operator ==(ObjectiveVector left, ObjectiveVector right) => left.Equals(right);
  public static bool operator !=(ObjectiveVector left, ObjectiveVector right) => !(left == right);
}
