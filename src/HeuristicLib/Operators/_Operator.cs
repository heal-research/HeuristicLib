namespace HEAL.HeuristicLib.Operators;

// public interface IOperator<TOperatorInstance> {
//   TOperatorInstance CreateInstance();
// }

public abstract record class Operator<TOperatorInstance> /*: IOperator<TOperatorInstance>*/ {
  public abstract TOperatorInstance CreateInstance();
}

// public interface IOperatorInstance<TOperator> {
//   TOperator Parameters { get; }
// }

public abstract class OperatorInstance<TOperator> /*: IOperatorInstance<TOperator>*/ {
  public TOperator Parameters { get; }
  
  protected OperatorInstance(TOperator parameters) {
    Parameters = parameters;
  }
}

[System.Runtime.CompilerServices.CollectionBuilder(typeof(ImmutableListBuilder), nameof(ImmutableListBuilder.Create))]
public record class ImmutableList<T> : IReadOnlyList<T>, IEquatable<ImmutableList<T>> 
  where T : IEquatable<T>
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
  
  public virtual bool Equals(ImmutableList<T>? other) {
    if (other == null) return false;
    if (Count != other.Count) return false;
    for (int i = 0; i < Count; i++) {
      if (!this[i].Equals(other[i])) return false;
    }
    return true;
  }
  public override int GetHashCode() {
    var hash = new HashCode();
    foreach (var terminator in items) {
      hash.Add(terminator);
    }
    return hash.ToHashCode();
  }
}

public static class ImmutableListBuilder {
  public static ImmutableList<T> Create<T>(ReadOnlySpan<T> items) where T : IEquatable<T> {
    return new ImmutableList<T>(items);
  }
}
