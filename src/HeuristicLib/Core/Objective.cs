using System.Collections;
using System.Globalization;
using HEAL.HeuristicLib.Encodings;

namespace HEAL.HeuristicLib;

public enum ObjectiveDirection {
  Minimize,
  Maximize 
}

public static class SingleObjective {
  public static readonly Objective Minimize = new Objective([ObjectiveDirection.Minimize], FitnessTotalOrderComparer.CreateSingleObjectiveComparer(ObjectiveDirection.Minimize));
  public static readonly Objective Maximize = new Objective([ObjectiveDirection.Maximize], FitnessTotalOrderComparer.CreateSingleObjectiveComparer(ObjectiveDirection.Maximize));
  public static Objective Create(ObjectiveDirection direction) => direction switch {
    ObjectiveDirection.Minimize => Minimize,
    ObjectiveDirection.Maximize => Maximize,
    _ => throw new NotImplementedException()
  };

  public static Objective WithSingleObjective(this Objective objectives) => Create(objectives.Directions.Single());
}

public static class MultiObjective {
  public static Objective Create(ObjectiveDirection[] directions) => new Objective(directions, new NoTotalOrderComparer());
  
  public static Objective WeightedSum(ObjectiveDirection[] directions, double[]? weights) => new(directions, new WeightedSumComparer(directions, weights));
  public static Objective Lexicographic(ObjectiveDirection[] directions, int[]? order) => new(directions, new LexicographicComparer(directions, order));
  
  public static Objective WithWithWeightedSum(this Objective objectives, double[] weights) => WeightedSum(objectives.Directions, weights);
  public static Objective WithLexicographicOrder(this Objective objectives, int[] order) => Lexicographic(objectives.Directions, order);
}

public static class FitnessTotalOrderComparer {
  public static SingleObjectiveComparer CreateSingleObjectiveComparer(ObjectiveDirection objectiveDirection) {
    return new SingleObjectiveComparer(objectiveDirection);
  }
  public static WeightedSumComparer CreateWeightedSumComparer(ObjectiveDirection[] objectives, double[]? weights = null) {
    return new WeightedSumComparer(objectives, weights);
  }
  public static LexicographicComparer CreateLexicographicComparer(ObjectiveDirection[] objectives, int[]? order = null) {
    return new LexicographicComparer(objectives, order);
  }
  public static NoTotalOrderComparer CreateNoTotalOrderComparer(ObjectiveDirection[] objectives) {
    return new NoTotalOrderComparer();
  }
}

public class SingleObjectiveComparer : IComparer<Fitness> {
  private readonly ObjectiveDirection objectiveDirection;
  
  public SingleObjectiveComparer(ObjectiveDirection objectiveDirection) {
    this.objectiveDirection = objectiveDirection;
  }
  
  public int Compare(Fitness? x, Fitness? y) {
    if (x is not null && !x.IsSingleObjective || y is not null && !y.IsSingleObjective) throw new ArgumentException("Fitness must be single-objective");
    if (x is null && y is null) return 0;
    if (x is null) return -1;
    if (y is null) return -1;
    if (ReferenceEquals(x, y)) return 0;
    
    return objectiveDirection switch {
      ObjectiveDirection.Minimize => x[0].CompareTo(y[0]),
      ObjectiveDirection.Maximize => y[0].CompareTo(x[0]),
      _ => throw new NotImplementedException()
    };
  }
}

public class WeightedSumComparer : IComparer<Fitness> {
  private readonly ObjectiveDirection[] objectives;
  private readonly RealVector weights;
  
  public WeightedSumComparer(ObjectiveDirection[] objectives, double[]? weights = null) {
    if (weights is not null && objectives.Length != weights.Length) throw new ArgumentException("Objective and weights must have the same length");
    this.objectives = objectives;
    this.weights = weights ?? RealVector.Repeat(1.0, this.objectives.Length);
  }
  public int Compare(Fitness? x, Fitness? y) {
    if (x is not null && x.Count != objectives.Length || y is not null && y.Count != objectives.Length) throw new ArgumentException("Fitness must have the same length as the objective");
    if (x is null && y is null) return 0;
    if (x is null) return -1;
    if (y is null) return -1;

    var xFitness = new RealVector(x);
    var yFitness = new RealVector(y);
    
    var directions = new RealVector(objectives.Select(d => d switch {
      ObjectiveDirection.Minimize => +1.0,
      ObjectiveDirection.Maximize => -1.0,
      _ => throw new NotImplementedException()
    }));
    var directedWeights = weights * directions;
    
    double xSum = (xFitness * directedWeights).Sum();
    double ySum = (yFitness * directedWeights).Sum();
    
    return xSum.CompareTo(ySum);
  }
}

public class LexicographicComparer : IComparer<Fitness> {
  private readonly ObjectiveDirection[] objectives;
  private readonly Permutation order;
  
  public LexicographicComparer(ObjectiveDirection[] objectives, int[]? order = null) {
    this.objectives = objectives;
    this.order = order ?? Permutation.Range(objectives.Length);
  }

  public int Compare(Fitness? x, Fitness? y) {
    if (x is not null && x.Count != objectives.Length() || y is not null && y.Count != objectives.Length) throw new ArgumentException("Fitness must have the same length as the objective");
    if (x is null && y is null) return 0;
    if (x is null) return -1;
    if (y is null) return -1;
    
    foreach (int dimension in order) {
      int comparison = x[dimension].CompareTo(y[dimension]);
      if (comparison != 0) return objectives[dimension] switch {
        ObjectiveDirection.Minimize => +comparison,
        ObjectiveDirection.Maximize => -comparison,
        _ => throw new NotImplementedException()
      };
    }
    return 0;
  }
}

public class NoTotalOrderComparer : IComparer<Fitness> {
  public int Compare(Fitness? x, Fitness? y) => throw new InvalidOperationException("No total order defined");
}



public sealed class Objective /*: IReadOnlyList<ObjectiveDirection>, IEquatable<Objective>*/ {
  public ObjectiveDirection[] Directions { get; }
  //public int Dimensions => Directions.Length;
  
  public IComparer<Fitness> TotalOrderComparer { get; }

  public Objective(ObjectiveDirection[] directions, IComparer<Fitness> totalOrderComparer) {
    if (directions.Length == 0) throw new ArgumentException("Direction vector must not be empty");
    Directions = directions;
    TotalOrderComparer = totalOrderComparer;
  }

  //public bool IsSingleObjective => Directions.Length == 1;
  //public ObjectiveDirection? SingleObjectiveDirection => Directions.SingleOrDefault();
  
  // public static implicit operator Objective(ObjectiveDirection objectiveDirection) {
  //   return new Objective([objectiveDirection], new SingleObjectiveComparer(objectiveDirection));
  // }
  
  // public IEnumerator<ObjectiveDirection> GetEnumerator() => ((IEnumerable<ObjectiveDirection>)directions).GetEnumerator();
  // IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
  // public int Count => directions.Length;
  // public ObjectiveDirection this[int index] => directions[index];
  //
  // public bool Equals(Objective? other) {
  //   if (other is null) return false;
  //   if (ReferenceEquals(this, other)) return true;
  //   if (Count != other.Count) return false;
  //   // ToDo: also compare TotalOrderComparer
  //   return directions.SequenceEqual(other.directions);
  // }
  // public override bool Equals(object? obj) => Equals(obj as Objective);
  // public override int GetHashCode() => directions.Aggregate(0, (hash, val) => HashCode.Combine(hash, val.GetHashCode()));
  //
  public override string ToString() => $"[{string.Join(", ", Directions.Select(d => d.ToString()))}]";
}



public readonly record struct SingleFitness(double Value) {
  public static implicit operator SingleFitness(double value) {
    return new SingleFitness(value);
  }

  // public static IComparer<Fitness> CreateSingleObjectiveComparer(ObjectiveDirection objectiveDirection) {
  //   return new FitnessValueComparer(objectiveDirection);
  // }
  
  // private sealed class FitnessValueComparer : IComparer<Fitness> {
  //   private readonly ObjectiveDirection objectiveDirection;
  //   public FitnessValueComparer(ObjectiveDirection objectiveDirection) {
  //     this.objectiveDirection = objectiveDirection;
  //   }
  //   public int Compare(Fitness? x, Fitness? y) {
  //     if (x is null && y is null) return 0; else if (x is null) return +1; else if (y is null) return -1;
  //     if (!x.IsSingleObjective || !y.IsSingleObjective) throw new ArgumentException("Fitness vectors must be single-objective");
  //
  //     return x.CompareTo(y[0], objectiveDirection) switch {
  //       DominanceRelation.Dominates => -1,
  //       DominanceRelation.IsDominatedBy => +1,
  //       DominanceRelation.Equivalent => 0,
  //       _ => throw new InvalidOperationException("Fitness vectors are incomparable")
  //     };
  //   }
  // }
  
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
//
// public interface IParetoComparer<in T> {
//   DominanceRelation CompareTo(T other);
// }

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
  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
  public int Count => values.Length;
  public double this[int index] => values[index];
  
  public bool Equals(Fitness? other) {
    if (other is null) return false;
    if (ReferenceEquals(this, other)) return true;
    if (Count != other.Count) return false;
    return values.SequenceEqual(other.values);
  }
  public override bool Equals(object? obj) => Equals(obj as Fitness);
  public override int GetHashCode() => values.Aggregate(0, (hash, val) => HashCode.Combine(hash, val.GetHashCode()));

  
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
