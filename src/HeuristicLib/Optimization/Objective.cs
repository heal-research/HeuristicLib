namespace HEAL.HeuristicLib.Optimization;

public sealed class Objective /*: IReadOnlyList<ObjectiveDirection>, IEquatable<Objective>*/ {
  public ObjectiveDirection[] Directions { get; }
  //public int Dimensions => Directions.Length;

  public IComparer<ObjectiveVector> TotalOrderComparer { get; }

  public Objective(ObjectiveDirection[] directions, IComparer<ObjectiveVector> totalOrderComparer) {
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
