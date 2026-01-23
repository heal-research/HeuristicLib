using System.Collections;

namespace HEAL.HeuristicLib.Genotypes.Vectors;

public class IntegerVector(params IEnumerable<int> elements) : IReadOnlyList<int>, IEquatable<IntegerVector>
{
  private readonly int[] elements = elements.ToArray();

  // public RealVector(double value) {
  //   elements = [value];
  // }

  public int Count => elements.Length;

  public static implicit operator IntegerVector(int value) => new(value);

  public static implicit operator IntegerVector(int[] values) => new(values);

  public int this[int index] => elements[index];

  public int this[Index index] => elements[index];

  public IEnumerator<int> GetEnumerator() => ((IEnumerable<int>)elements).GetEnumerator();

  IEnumerator IEnumerable.GetEnumerator() => elements.GetEnumerator();

  public bool Equals(IntegerVector? other)
  {
    if (other is null) {
      return false;
    }

    return ReferenceEquals(this, other)
           || elements.SequenceEqual(other.elements);
  }

  public override int GetHashCode()
  {
    var hash = new HashCode();
    foreach (var element in elements) {
      hash.Add(element);
    }

    return hash.ToHashCode();
  }

  public static implicit operator RealVector(IntegerVector integerVector)
  {
    return new RealVector(integerVector.elements.Select(i => (double)i));
  }
}
