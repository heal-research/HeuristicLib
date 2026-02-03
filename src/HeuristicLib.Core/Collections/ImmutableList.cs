using System.Collections;
using System.Runtime.CompilerServices;

namespace HEAL.HeuristicLib.Collections;

[CollectionBuilder(typeof(ImmutableListBuilder), nameof(ImmutableListBuilder.Create))]
public class ImmutableList<T> : IReadOnlyList<T>, IEquatable<ImmutableList<T>>
// where T : IEquatable<T>
{
  private readonly T[] items;

  public ImmutableList(IEnumerable<T> items) => this.items = items.ToArray();

  public ImmutableList(ReadOnlySpan<T> items) => this.items = items.ToArray();

  public virtual bool Equals(ImmutableList<T>? other)
  {
    if (other is null) {
      return false;
    }

    if (Count != other.Count) {
      return false;
    }

    for (var i = 0; i < Count; i++) {
      if (!this[i]!.Equals(other[i])) {
        return false;
      }
    }

    return true;
  }

  public T this[int index] => items[index];
  public int Count => items.Length;
  public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)items).GetEnumerator();
  IEnumerator IEnumerable.GetEnumerator() => items.GetEnumerator();

  public override bool Equals(object? obj)
  {
    if (obj is null) {
      return false;
    }

    if (ReferenceEquals(this, obj)) {
      return true;
    }

    return obj.GetType() == GetType() && Equals((ImmutableList<T>)obj);
  }

  public override int GetHashCode()
  {
    var hash = new HashCode();
    foreach (var terminator in items) {
      hash.Add(terminator);
    }

    return hash.ToHashCode();
  }

  public static bool operator ==(ImmutableList<T> left, ImmutableList<T> right) => left.Equals(right);

  public static bool operator !=(ImmutableList<T> left, ImmutableList<T> right) => !(left == right);
}
