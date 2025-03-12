using System.Collections;

namespace HEAL.HeuristicLib.Algorithms;

public enum Goal {
  Minimize,
  Maximize
}

public readonly record struct Fitness(double Value) {
  public static implicit operator Fitness(double value) {
    return new Fitness(value);
  }

  public static IComparer<Fitness> CreateSingleObjectiveComparer(Goal goal) {
    return new FitnessValueComparer(goal);
  }
  
  private sealed class FitnessValueComparer : IComparer<Fitness> {
    private readonly Goal goal;
    public FitnessValueComparer(Goal goal) {
      this.goal = goal;
    }
    public int Compare(Fitness x, Fitness y) => x.CompareTo(y, goal);
  }
  
  public int CompareTo(Fitness other, Goal goal) => goal switch {
    Goal.Minimize => Value.CompareTo(other.Value),
    Goal.Maximize => other.Value.CompareTo(Value),
    _ => throw new NotImplementedException()
  };

  public bool IsBetterThan(Fitness other, Goal goal) => CompareTo(other, goal) < 0;
  public bool IsWorseThan(Fitness other, Goal goal) => CompareTo(other, goal) > 0;
  public bool IsEqualTo(Fitness other, Goal goal) => CompareTo(other, goal) == 0;

  public override string ToString() => $"{Value}";

  public static Fitness GetBest(Goal goal) => goal switch {
    Goal.Minimize => new Fitness(double.MinValue),
    Goal.Maximize => new Fitness(double.MaxValue),
    _ => throw new NotImplementedException()
  };
  public static Fitness GetWorst(Goal goal) => goal switch {
    Goal.Minimize => new Fitness(double.MaxValue),
    Goal.Maximize => new Fitness(double.MinValue),
    _ => throw new NotImplementedException()
  };
  
}



public enum DominanceRelation {
  Dominates,
  IsDominatedBy,
  Equivalent,
  Incomparable
}

public interface IParetoComparer<in T> {
  DominanceRelation CompareTo(T other);
}

public sealed class FitnessVector : IReadOnlyList<Fitness>, IEquatable<FitnessVector> {
  private readonly Fitness[] values;
  
  public FitnessVector(params IEnumerable<Fitness> values) {
    this.values = values.ToArray();
    if (this.values.Length() == 0) throw new ArgumentException("Fitness vector must not be empty");
  }
  
  public bool IsSingleObjective => Count == 1;
  
  public static implicit operator FitnessVector(Fitness value) {
    return new FitnessVector(value);
  }
  public static implicit operator FitnessVector(double value) {
    return new FitnessVector(value);
  }

  public IEnumerator<Fitness> GetEnumerator() => ((IEnumerable<Fitness>)values).GetEnumerator();
  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
  public int Count => values.Length;
  public Fitness this[int index] => values[index];
  
  public bool Equals(FitnessVector? other) {
    if (other is null) return false;
    if (ReferenceEquals(this, other)) return true;
    if (Count != other.Count) return false;
    return values.SequenceEqual(other.values);
  }
  public override bool Equals(object? obj) => Equals(obj as FitnessVector);
  public override int GetHashCode() => values.Aggregate(0, (hash, val) => HashCode.Combine(hash, val.GetHashCode()));

  public DominanceRelation CompareTo(FitnessVector? other, GoalVector goals) {
    if (ReferenceEquals(this, other)) return 0;
    ArgumentNullException.ThrowIfNull(other);
    if (Count != other.Count) throw new ArgumentException("Fitness vectors must have the same length");
    if (Count != goals.Count) throw new ArgumentException("Fitness vectors and directions must have the same length");

    int[] comparisons = new int[Count];
    for (int i = 0; i < Count; i++) {
      comparisons[i] = this[i].CompareTo(other[i], goals[i]);
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

  public bool Dominates(FitnessVector other, GoalVector goals) => CompareTo(other, goals) == DominanceRelation.Dominates;
  public bool IsDominatedBy(FitnessVector other, GoalVector goals) => CompareTo(other, goals) == DominanceRelation.IsDominatedBy;
  public bool IsEquivalentTo(FitnessVector other, GoalVector goals) => CompareTo(other, goals) == DominanceRelation.Equivalent;
  public bool IsIncomparableTo(FitnessVector other, GoalVector goals) => CompareTo(other, goals) == DominanceRelation.Incomparable;

  public override string ToString() => $"[{string.Join(", ", values.Select(v => v.ToString()))}]";

  public static IComparer<FitnessVector> CreateWeightedSumComparer(GoalVector goals, params IEnumerable<double> weights) {
    return new WeightedSumComparer(goals, weights);
  }
  
  private sealed class WeightedSumComparer : IComparer<FitnessVector> {
    private readonly GoalVector goals;
    private readonly double[] weights;
    public WeightedSumComparer(GoalVector goals, IEnumerable<double> weights) {
      this.goals = goals;
      this.weights = weights.ToArray();
    }
    public int Compare(FitnessVector? x, FitnessVector? y) {
      ArgumentNullException.ThrowIfNull(x);
      ArgumentNullException.ThrowIfNull(y);
      if (ReferenceEquals(x, y)) return 0;
      if (x.Count != y.Count) throw new ArgumentException("Fitness vectors must have the same length");
      if (x.Count != goals.Count) throw new ArgumentException("Fitness vectors and directions must have the same length");

      double[] directionFactors = goals.Select(d => d switch {
        Goal.Minimize => +1.0,
        Goal.Maximize => -1.0,
        _ => throw new NotImplementedException()
      }).ToArray();
      
      double xSum = 0, ySum = 0;
      for (int i = 0; i < x.Count; i++) {
        xSum += x[i].Value * weights[i] * directionFactors[i];
        ySum += y[i].Value * weights[i] * directionFactors[i];
      }
      
      return xSum.CompareTo(ySum);
    }
  }
}

public sealed class GoalVector : IReadOnlyList<Goal>, IEquatable<GoalVector> {
  private readonly Goal[] directions;
  
  public GoalVector(params IEnumerable<Goal> directions) {
    this.directions = directions.ToArray();
    if (this.directions.Length == 0) throw new ArgumentException("Direction vector must not be empty");
  }

  public bool IsSingleObjective => Count == 1;
  
  public static implicit operator GoalVector(Goal goal) {
    return new GoalVector(goal);
  } 
  
  public IEnumerator<Goal> GetEnumerator() => ((IEnumerable<Goal>)directions).GetEnumerator();
  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
  public int Count => directions.Length;
  public Goal this[int index] => directions[index];
  
  public bool Equals(GoalVector? other) {
    if (other is null) return false;
    if (ReferenceEquals(this, other)) return true;
    if (Count != other.Count) return false;
    return directions.SequenceEqual(other.directions);
  }
  public override bool Equals(object? obj) => Equals(obj as GoalVector);
  public override int GetHashCode() => directions.Aggregate(0, (hash, val) => HashCode.Combine(hash, val.GetHashCode()));
  
  public override string ToString() => $"[{string.Join(", ", directions.Select(d => d.ToString()))}]";
}
