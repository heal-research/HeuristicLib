namespace HEAL.HeuristicLib.Encodings.IntegerVector;

public sealed class IntegerVector(params IEnumerable<int> elements) : IReadOnlyList<int>, IEquatable<IntegerVector> {
  private readonly int[] elements = elements.ToArray();

  public int Count => elements.Length;

  public static implicit operator IntegerVector(int value) => new(value);

  public static implicit operator IntegerVector(int[] values) => new(values);

  public int this[int index] => elements[index];

  public int this[Index index] => elements[index];

  public IEnumerator<int> GetEnumerator() => ((IEnumerable<int>)elements).GetEnumerator();

  System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => elements.GetEnumerator();

  public override bool Equals(object? obj) => obj is IntegerVector other && Equals(other);

  public bool Equals(IntegerVector? other) => other is not null && (ReferenceEquals(this, other) || elements.SequenceEqual(other.elements));

  public override int GetHashCode() {
    var hash = new HashCode();
    foreach (var element in elements)
      hash.Add(element);
    return hash.ToHashCode();
  }

  public static implicit operator RealVector.RealVector(IntegerVector integerVector) => new(integerVector.elements.Select(i => (double)i));
}
