using System.Globalization;

namespace HEAL.HeuristicLib;

public readonly record struct SingleFitness(double Value) {
  public static implicit operator SingleFitness(double value) {
    return new SingleFitness(value);
  }
  
  public int CompareTo(SingleFitness other, ObjectiveDirection objectiveDirection) => objectiveDirection switch {
    ObjectiveDirection.Minimize => Value.CompareTo(other.Value),
    ObjectiveDirection.Maximize => other.Value.CompareTo(Value),
    _ => throw new NotImplementedException()
  };

  public bool IsBetterThan(SingleFitness other, ObjectiveDirection objectiveDirection) => CompareTo(other, objectiveDirection) < 0;
  public bool IsWorseThan(SingleFitness other, ObjectiveDirection objectiveDirection) => CompareTo(other, objectiveDirection) > 0;
  public bool IsEqualTo(SingleFitness other, ObjectiveDirection objectiveDirection) => CompareTo(other, objectiveDirection) == 0;

  public override string ToString() => $"{Value}";

  public static SingleFitness BestValue(ObjectiveDirection objectiveDirection) => objectiveDirection switch {
    ObjectiveDirection.Minimize => new SingleFitness(double.MinValue),
    ObjectiveDirection.Maximize => new SingleFitness(double.MaxValue),
    _ => throw new NotImplementedException()
  };
  public static SingleFitness WorstValue(ObjectiveDirection objectiveDirection) => objectiveDirection switch {
    ObjectiveDirection.Minimize => new SingleFitness(double.MaxValue),
    ObjectiveDirection.Maximize => new SingleFitness(double.MinValue),
    _ => throw new NotImplementedException()
  };
  
}



public enum DominanceRelation {
  Dominates,
  IsDominatedBy,
  Equivalent,
  Incomparable
}

public sealed class Fitness : IReadOnlyList<double>, IEquatable<Fitness> {
  private readonly double[] values;
  
  public Fitness(params double[] values) {
    if (values.Length() == 0) throw new ArgumentException("Fitness vector must not be empty");
    this.values = values.ToArray();
  }
  public Fitness(params IEnumerable<double> values) {
    this.values = values.ToArray();
    if (this.values.Length() == 0) throw new ArgumentException("Fitness vector must not be empty");
  }
  
  //public static implicit operator Fitness(SingleFitness[] values) => new(values);
  public static implicit operator Fitness(double[] values) => new(values/*.Select(v => new SingleFitness(v))*/);
  //public static implicit operator Fitness(SingleFitness value) => new(value);
  public static implicit operator Fitness(double value) => new(value);

  public bool IsSingleObjective => Count == 1;
  public SingleFitness? SingleFitness => values.SingleOrDefault();
  


  // public IEnumerator<SingleFitness> GetEnumerator() => ((IEnumerable<SingleFitness>)values).GetEnumerator();
  // IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
  // public int Count => values.Length;
  // public SingleFitness this[int index] => values[index];
  public IEnumerator<double> GetEnumerator() => ((IEnumerable<double>)values).GetEnumerator();
  System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
  public int Count => values.Length;
  public double this[int index] => values[index];
  
  public bool Equals(Fitness? other) {
    if (other is null) return false;
    if (ReferenceEquals(this, other)) return true;
    if (Count != other.Count) return false;
    return values.SequenceEqual(other.values);
  }
  public override bool Equals(object? obj) => Equals(obj as Fitness);
  public override int GetHashCode() => values.Aggregate(0, HashCode.Combine);

  
  public DominanceRelation CompareTo(Fitness? other, Objective objective) {
    if (ReferenceEquals(this, other)) return 0;
    ArgumentNullException.ThrowIfNull(other);
    if (Count != other.Count) throw new ArgumentException("Fitness vectors must have the same length");
    if (Count != objective.Directions.Length) throw new ArgumentException("Fitness vectors and directions must have the same length");
    
    int[] comparisons = new int[Count];
    for (int i = 0; i < Count; i++) {
      comparisons[i] = this[i].CompareTo(other[i]);
      comparisons[i] *= objective.Directions[i] switch { ObjectiveDirection.Minimize => +1, ObjectiveDirection.Maximize => -1, _ => throw new NotImplementedException() };
    }
    bool thisNotWorse = comparisons.All(c => c <= 0);
    bool otherNotWorse = comparisons.All(c => c >= 0);

    return (thisNotWorse, otherNotWorse) switch {
      (true, true) => DominanceRelation.Equivalent,
      (true, false) => DominanceRelation.Dominates,
      (false, true) => DominanceRelation.IsDominatedBy,
      _ => DominanceRelation.Incomparable
    };
  }

  public bool Dominates(Fitness other, Objective goals) => CompareTo(other, goals) == DominanceRelation.Dominates;
  public bool IsDominatedBy(Fitness other, Objective goals) => CompareTo(other, goals) == DominanceRelation.IsDominatedBy;
  public bool IsEquivalentTo(Fitness other, Objective goals) => CompareTo(other, goals) == DominanceRelation.Equivalent;
  public bool IsIncomparableTo(Fitness other, Objective goals) => CompareTo(other, goals) == DominanceRelation.Incomparable;

  public override string ToString() => $"[{string.Join(", ", values.Select(v => v.ToString(CultureInfo.InvariantCulture)))}]";
}
