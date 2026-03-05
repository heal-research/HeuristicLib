namespace HEAL.HeuristicLib.Optimization;

public sealed class Objective
{
  public ObjectiveDirection[] Directions { get; }
  //public int Dimensions => Directions.Length;

  public IComparer<ObjectiveVector> TotalOrderComparer { get; }
  public ObjectiveVector Worst { get; }

  public Objective(ObjectiveDirection[] directions, IComparer<ObjectiveVector> totalOrderComparer)
  {
    if (directions.Length == 0) {
      throw new ArgumentException("Direction vector must not be empty");
    }

    Directions = directions;
    TotalOrderComparer = totalOrderComparer;
    Worst = new ObjectiveVector(directions.Select(d => d == ObjectiveDirection.Minimize ? double.PositiveInfinity : double.NegativeInfinity));
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

public static class ObjectiveExtensions
{
  extension(IEnumerable<ObjectiveVector> values)
  {
    public ObjectiveVector Best(Objective o) => values.Min(o.TotalOrderComparer);

    public ObjectiveVector Worst(Objective o) => values.Max(o.TotalOrderComparer);

    public ObjectiveVector Median(Objective o) => values.Median(o.TotalOrderComparer);
  }

  extension<T>(IEnumerable<T> source)
  {
    public T Min(IComparer<T> comparer)
    {
      ArgumentNullException.ThrowIfNull(source);
      ArgumentNullException.ThrowIfNull(comparer);

      using var e = source.GetEnumerator();
      if (!e.MoveNext())
        throw new InvalidOperationException("Sequence contains no elements.");

      var best = e.Current;

      while (e.MoveNext()) {
        if (comparer.Compare(e.Current, best) < 0)
          best = e.Current;
      }

      return best;
    }

    public T Max(IComparer<T> comparer)
    {
      ArgumentNullException.ThrowIfNull(source);
      ArgumentNullException.ThrowIfNull(comparer);

      using var e = source.GetEnumerator();
      if (!e.MoveNext())
        throw new InvalidOperationException("Sequence contains no elements.");

      var best = e.Current;

      while (e.MoveNext()) {
        if (comparer.Compare(e.Current, best) > 0)
          best = e.Current;
      }

      return best;
    }

    public T Median(IComparer<T> comparer)
    {
      ArgumentNullException.ThrowIfNull(source);
      ArgumentNullException.ThrowIfNull(comparer);

      var arr = source.ToArray();
      if (arr.Length == 0)
        throw new InvalidOperationException("Sequence contains no elements.");

      int k = arr.Length / 2;
      return QuickSelect(arr, 0, arr.Length - 1, k, comparer);
    }
  }

  private static T QuickSelect<T>(T[] arr, int left, int right, int k, IComparer<T> cmp)
  {
    while (true) {
      if (left == right)
        return arr[left];

      int pivotIndex = Partition(arr, left, right, (left + right) / 2, cmp);

      if (k == pivotIndex)
        return arr[k];
      if (k < pivotIndex)
        right = pivotIndex - 1;
      else
        left = pivotIndex + 1;
    }
  }

  private static int Partition<T>(T[] arr, int left, int right, int pivotIndex, IComparer<T> cmp)
  {
    var pivotValue = arr[pivotIndex];
    Swap(arr, pivotIndex, right);

    int storeIndex = left;

    for (int i = left; i < right; i++) {
      if (cmp.Compare(arr[i], pivotValue) >= 0)
        continue;

      Swap(arr, storeIndex, i);
      storeIndex++;
    }

    Swap(arr, right, storeIndex);
    return storeIndex;
  }

  private static void Swap<T>(T[] arr, int i, int j)
  {
    (arr[i], arr[j]) = (arr[j], arr[i]);
  }
}
