using System.Collections;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Genotypes.Vectors;

public sealed class IntegerVector(params IEnumerable<int> elements) : IReadOnlyList<int>, IEquatable<IntegerVector>
{
  private readonly int[] elements = elements.ToArray();

  public int Count => elements.Length;

  public static implicit operator IntegerVector(int value) => new(value);

  public static implicit operator IntegerVector(int[] values) => new(values);

  public int this[int index] => elements[index];

  public int this[Index index] => elements[index];

  public IEnumerator<int> GetEnumerator() => ((IEnumerable<int>)elements).GetEnumerator();

  IEnumerator IEnumerable.GetEnumerator() => elements.GetEnumerator();

  public bool Equals(IntegerVector? other) => other is not null && (ReferenceEquals(this, other) || elements.SequenceEqual(other.elements));
  public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is IntegerVector other && Equals(other);

  public override int GetHashCode()
  {
    var hash = new HashCode();
    foreach (var element in elements) {
      hash.Add(element);
    }

    return hash.ToHashCode();
  }

  public static implicit operator RealVector(IntegerVector integerVector) => new(integerVector.elements.Select(i => (double)i));

  public static BoolVector operator >(IntegerVector a, IntegerVector b)
  {
    AssertComparable(a, b);

    var length = BroadcastLength(a, b);
    var result = new bool[length];

    for (var i = 0; i < length; i++) {
      var aValue = a.Count == 1 ? a[0] : a[i];
      var bValue = b.Count == 1 ? b[0] : b[i];
      result[i] = aValue > bValue;
    }

    return new BoolVector(result);
  }

  public static BoolVector operator <(IntegerVector a, IntegerVector b)
  {
    AssertComparable(a, b);

    var length = BroadcastLength(a, b);
    var result = new bool[length];

    for (var i = 0; i < length; i++) {
      var aValue = a.Count == 1 ? a[0] : a[i];
      var bValue = b.Count == 1 ? b[0] : b[i];
      result[i] = aValue < bValue;
    }

    return new BoolVector(result);
  }

  public static BoolVector operator >=(IntegerVector a, IntegerVector b)
  {
    AssertComparable(a, b);

    var length = BroadcastLength(a, b);
    var result = new bool[length];

    for (var i = 0; i < length; i++) {
      var aValue = a.Count == 1 ? a[0] : a[i];
      var bValue = b.Count == 1 ? b[0] : b[i];
      result[i] = aValue >= bValue;
    }

    return new BoolVector(result);
  }

  public static BoolVector operator <=(IntegerVector a, IntegerVector b)
  {
    AssertComparable(a, b);

    var length = BroadcastLength(a, b);
    var result = new bool[length];

    for (var i = 0; i < length; i++) {
      var aValue = a.Count == 1 ? a[0] : a[i];
      var bValue = b.Count == 1 ? b[0] : b[i];
      result[i] = aValue <= bValue;
    }

    return new BoolVector(result);
  }

  public static bool operator ==(IntegerVector a, IntegerVector b) => a.Equals(b);
  public static bool operator !=(IntegerVector a, IntegerVector b) => !a.Equals(b);

  public static bool AreCompatible(IntegerVector a, IntegerVector b) => a.Count == b.Count || a.Count == 1 || b.Count == 1;

  private static void AssertComparable(IntegerVector a, IntegerVector b)
  {
    var test = AreCompatible(a, b);
    if (!test)
      throw new ArgumentException($"Integer vectors {a} and {b} are not compatible");
  }

  private static void AssertComparable(int length, IntegerVector b)
  {
    var test = b.Count == length || b.Count == 1;
    if (!test)
      throw new ArgumentException($"Integer vector and {b} is not compatible with length {length}");
  }

  public static int BroadcastLength(RealVector a, RealVector b) => Math.Max(a.Count, b.Count);

  public static IntegerVector CreateUniform(int length, IntegerVector low, IntegerVector high, IRandomNumberGenerator random)
  {
    AssertComparable(length, low);
    AssertComparable(length, high);
    var result = new int[length];
    for (var i = 0; i < length; i++) {
      var min = low.Count == 1 ? low[0] : low[i];
      var max = high.Count == 1 ? high[0] : high[i];
      result[i] = random.NextInt(min, max, true);
    }

    return new IntegerVector(result);
  }

  public IntegerVector Clamp(IntegerVector low, IntegerVector high)
  {
    AssertComparable(this, low);
    AssertComparable(this, high);
    var result = new int[elements.Length];
    for (var i = 0; i < elements.Length; i++) {
      var min = low.Count == 1 ? low[0] : low[i];
      var max = high.Count == 1 ? high[0] : high[i];
      result[i] = Math.Max(min, Math.Max(max, elements[i]));
    }

    return new IntegerVector(result);
  }
}
