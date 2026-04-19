using System.Collections;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Genotypes.Vectors;

public sealed class IntegerVector(params IEnumerable<int> elements) : IReadOnlyList<int>, IEquatable<IntegerVector>
{
  private readonly int[] elements = elements.ToArray();

  public int Count => elements.Length;

  public int this[int index] => elements[index];

  public int this[Index index] => elements[index];

  public IEnumerator<int> GetEnumerator() => ((IEnumerable<int>)elements).GetEnumerator();

  IEnumerator IEnumerable.GetEnumerator() => elements.GetEnumerator();

  public bool Equals(IntegerVector? other) =>
    other is not null && (ReferenceEquals(this, other) || elements.SequenceEqual(other.elements));

  public override bool Equals(object? obj) =>
    obj is IntegerVector other && Equals(other);

  public override int GetHashCode()
  {
    var hash = new HashCode();
    foreach (var element in elements) {
      hash.Add(element);
    }

    return hash.ToHashCode();
  }

  public override string ToString() => $"[{string.Join(", ", elements)}]";

  public RealVector ToRealVector() => ToRealVector(this);

  public double ToRealAt(int dimension) => ToRealAt(this, dimension);

  public IntegerVector Clamp(IntegerVector? min, IntegerVector? max) => Clamp(this, min, max);

  public int ClampAt(IntegerVector? min, IntegerVector? max, int dimension) => ClampAt(this, min, max, dimension);

  public static implicit operator IntegerVector(int value) => new(value);

  public static implicit operator IntegerVector(int[] values) => new(values);

  public static implicit operator RealVector(IntegerVector integerVector) => ToRealVector(integerVector);

  public static IntegerVector CreateUniform(int length, IntegerVector low, IntegerVector high, IRandomNumberGenerator random)
    => random.NextIntVector(low, high, length);

  public static RealVector ToRealVector(IntegerVector input)
  {
    var result = new double[input.Count];
    for (var i = 0; i < input.Count; i++) {
      result[i] = input[i];
    }

    return new RealVector(result);
  }

  public static double ToRealAt(IntegerVector input, int dimension)
  {
    ArgumentOutOfRangeException.ThrowIfNegative(dimension);
    ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(dimension, input.Count);
    return input[dimension];
  }

  public static IntegerVector Add(IntegerVector a, IntegerVector b)
  {
    if (!AreCompatible(a, b))
      throw new ArgumentException("Vectors must be of the same length or one of length one");

    var length = BroadcastLength(a, b);
    var result = new int[length];
    for (var i = 0; i < length; i++) {
      var left = a.Count == 1 ? a[0] : a[i];
      var right = b.Count == 1 ? b[0] : b[i];
      result[i] = left + right;
    }

    return new IntegerVector(result);
  }

  public static IntegerVector Subtract(IntegerVector a, IntegerVector b)
  {
    if (!AreCompatible(a, b))
      throw new ArgumentException("Vectors must be of the same length or one of length one");

    var length = BroadcastLength(a, b);
    var result = new int[length];
    for (var i = 0; i < length; i++) {
      var left = a.Count == 1 ? a[0] : a[i];
      var right = b.Count == 1 ? b[0] : b[i];
      result[i] = left - right;
    }

    return new IntegerVector(result);
  }

  public static IntegerVector Multiply(IntegerVector a, IntegerVector b)
  {
    if (!AreCompatible(a, b))
      throw new ArgumentException("Vectors must be of the same length or one of length one");

    var length = BroadcastLength(a, b);
    var result = new int[length];
    for (var i = 0; i < length; i++) {
      var left = a.Count == 1 ? a[0] : a[i];
      var right = b.Count == 1 ? b[0] : b[i];
      result[i] = left * right;
    }

    return new IntegerVector(result);
  }

  public static IntegerVector Divide(IntegerVector a, IntegerVector b)
  {
    if (!AreCompatible(a, b))
      throw new ArgumentException("Vectors must be of the same length or one of length one");

    var length = BroadcastLength(a, b);
    var result = new int[length];
    for (var i = 0; i < length; i++) {
      var left = a.Count == 1 ? a[0] : a[i];
      var right = b.Count == 1 ? b[0] : b[i];
      result[i] = left / right;
    }

    return new IntegerVector(result);
  }

  public static IntegerVector Clamp(IntegerVector input, IntegerVector? min, IntegerVector? max)
  {
    if (min is null && max is null)
      return input;

    ValidateBounds(min, max, input.Count);

    var result = new int[input.Count];
    var changed = false;
    for (var i = 0; i < input.Count; i++) {
      result[i] = ClampAt(input[i], min, max, i);
      changed |= result[i] != input[i];
    }

    return changed ? new IntegerVector(result) : input;
  }

  public static int ClampAt(IntegerVector input, IntegerVector? min, IntegerVector? max, int dimension)
  {
    ValidateBounds(min, max, input.Count, dimension);

    return ClampAt(input[dimension], min, max, dimension);
  }

  public static int ClampAt(int value, IntegerVector? min, IntegerVector? max, int dimension)
  {
    ValidateBounds(min, max, dimension: dimension);

    var lower = int.MinValue;
    if (min is not null)
      lower = min.Count == 1 ? min[0] : min[dimension];

    var upper = int.MaxValue;
    if (max is not null)
      upper = max.Count == 1 ? max[0] : max[dimension];

    return Math.Clamp(value, lower, upper);
  }

  public static bool AreCompatible(IntegerVector a, IntegerVector b) => a.Count == b.Count || a.Count == 1 || b.Count == 1;

  public static int BroadcastLength(IntegerVector a, IntegerVector b) => Math.Max(a.Count, b.Count);

  public static IntegerVector operator +(IntegerVector a, IntegerVector b) => Add(a, b);

  public static IntegerVector operator -(IntegerVector a, IntegerVector b) => Subtract(a, b);

  public static IntegerVector operator *(IntegerVector a, IntegerVector b) => Multiply(a, b);

  public static IntegerVector operator /(IntegerVector a, IntegerVector b) => Divide(a, b);

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

  public static bool operator ==(IntegerVector? a, IntegerVector? b)
  {
    if (ReferenceEquals(a, b)) {
      return true;
    }

    if (a is null || b is null) {
      return false;
    }

    return a.Equals(b);
  }

  public static bool operator !=(IntegerVector? a, IntegerVector? b) => !(a == b);

  private static void AssertComparable(IntegerVector a, IntegerVector b)
  {
    var test = AreCompatible(a, b);
    if (!test)
      throw new ArgumentException($"Integer vectors {a} and {b} are not compatible");
  }

  private static void ValidateBounds(IntegerVector? minimum, IntegerVector? maximum, int? length = null, int? dimension = null)
  {
    if (length is not null) {
      if (minimum is not null && minimum.Count != 1 && minimum.Count != length)
        throw new ArgumentException($"Min vector must be of length 1 or match input length ({length})", nameof(minimum));
      if (maximum is not null && maximum.Count != 1 && maximum.Count != length)
        throw new ArgumentException($"Max vector must be of length 1 or match input length ({length})", nameof(maximum));
    }

    if (dimension is not null) {
      ArgumentOutOfRangeException.ThrowIfNegative(dimension.Value);
      if (length is not null)
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(dimension.Value, length.Value);
      if (minimum is not null && minimum.Count != 1 && dimension >= minimum.Count)
        throw new ArgumentOutOfRangeException(nameof(dimension), "Dimension must be within the minimum bounds vector or minimum must be scalar.");
      if (maximum is not null && maximum.Count != 1 && dimension >= maximum.Count)
        throw new ArgumentOutOfRangeException(nameof(dimension), "Dimension must be within the maximum bounds vector or maximum must be scalar.");
    }

    if (minimum is null || maximum is null)
      return;

    var broadcastLength = dimension is not null ? 1 : Math.Max(minimum.Count, maximum.Count);
    for (var i = 0; i < broadcastLength; i++) {
      var index = dimension ?? i;
      var lower = minimum.Count == 1 ? minimum[0] : minimum[index];
      var upper = maximum.Count == 1 ? maximum[0] : maximum[index];
      if (lower > upper)
        throw new ArgumentException("Minimum values must be less than or equal to maximum values.");
    }
  }
}
