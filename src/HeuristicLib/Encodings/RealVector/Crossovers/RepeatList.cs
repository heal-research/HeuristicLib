using System.Collections;

namespace HEAL.HeuristicLib.Encodings.RealVector.Crossovers;

public static class RepeatList {
  public static IReadOnlyList<T> Repeat<T>(this T v, int n) => new RepeatList<T>(v, n);
}

public sealed class RepeatList<T> : IReadOnlyList<T> {
  private readonly T value;

  public RepeatList(T value, int count) {
    if (count < 0)
      throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative.");
    this.value = value;
    Count = count;
  }

  public int Count { get; }

  public T this[int index] {
    get {
      ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count);
      return value;
    }
  }

  public IEnumerator<T> GetEnumerator() {
    for (var i = 0; i < Count; i++)
      yield return value;
  }

  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
