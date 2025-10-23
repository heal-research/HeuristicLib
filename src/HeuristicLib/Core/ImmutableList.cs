namespace HEAL.HeuristicLib.Core;

[System.Runtime.CompilerServices.CollectionBuilder(typeof(ImmutableListBuilder), nameof(ImmutableListBuilder.Create))]
public class ImmutableList<T> : IReadOnlyList<T>, IEquatable<ImmutableList<T>>
// where T : IEquatable<T>
{
  private readonly T[] items;

  public ImmutableList(IEnumerable<T> items) {
    this.items = items.ToArray();
  }

  public ImmutableList(ReadOnlySpan<T> items) {
    this.items = items.ToArray();
  }

  public T this[int index] => items[index];
  public int Count => items.Length;
  public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)items).GetEnumerator();
  System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => items.GetEnumerator();

  public override bool Equals(object? obj) {
    if (obj is null) return false;
    if (ReferenceEquals(this, obj)) return true;
    return obj.GetType() == GetType() && Equals((ImmutableList<T>)obj);
  }

  public virtual bool Equals(ImmutableList<T>? other) {
    if (other is null) return false;
    if (Count != other.Count) return false;
    for (int i = 0; i < Count; i++)
      if (!this[i]!.Equals(other[i]))
        return false;
    return true;
  }

  public override int GetHashCode() {
    var hash = new HashCode();
    foreach (var terminator in items)
      hash.Add(terminator);
    return hash.ToHashCode();
  }

  public static bool operator ==(ImmutableList<T> left, ImmutableList<T> right) {
    return left.Equals(right);
  }

  public static bool operator !=(ImmutableList<T> left, ImmutableList<T> right) {
    return !(left == right);
  }
}
