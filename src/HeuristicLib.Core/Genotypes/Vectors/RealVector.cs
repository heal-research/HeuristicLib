using System.Collections;
using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Genotypes.Vectors;

[SuppressMessage("Blocker Code Smell", "S3877:Exceptions should not be thrown from unexpected methods")]
public sealed class RealVector(params IEnumerable<double> elements) : IReadOnlyList<double>, IEquatable<RealVector>
{
  private readonly double[] elements = elements.ToArray();

  // public RealVector(double value) {
  //   elements = [value];
  // }

  public static implicit operator RealVector(double value) => new(value);

  public static implicit operator RealVector(double[] values) => new(values);
  //public static implicit operator RealVector?(double[]? values) => values is not null ? new RealVector(values) : null;

  //public static implicit operator RealVector(IntegerVector intVector) => new RealVector(intVector);

  public double this[int index] => elements[index];

  public double this[Index index] => elements[index];

  public IEnumerator<double> GetEnumerator() => ((IEnumerable<double>)elements).GetEnumerator();

  IEnumerator IEnumerable.GetEnumerator() => elements.GetEnumerator();

  public static RealVector Add(RealVector a, RealVector b)
  {
    if (a.elements.Length != b.elements.Length) {
      if (a.elements.Length == 1) {
        return new RealVector(b.elements.Select(x => x + a.elements[0]));
      }

      if (b.elements.Length == 1) {
        return new RealVector(a.elements.Select(x => x + b.elements[0]));
      }

      throw new ArgumentException("Vectors must be of the same length or one of length one");
    }

    var result = new double[a.elements.Length];
    for (var i = 0; i < a.elements.Length; i++) {
      result[i] = a.elements[i] + b.elements[i];
    }

    return new RealVector(result);
  }

  public static RealVector Subtract(RealVector a, RealVector b)
  {
    if (a.elements.Length != b.elements.Length) {
      if (a.elements.Length == 1) {
        return new RealVector(b.elements.Select(x => x - a.elements[0]));
      }

      if (b.elements.Length == 1) {
        return new RealVector(a.elements.Select(x => x - b.elements[0]));
      }

      throw new ArgumentException("Vectors must be of the same length or one of length one");
    }

    var result = new double[a.elements.Length];
    for (var i = 0; i < a.elements.Length; i++) {
      result[i] = a.elements[i] - b.elements[i];
    }

    return new RealVector(result);
  }

  public static RealVector Multiply(RealVector a, RealVector b)
  {
    if (a.elements.Length != b.elements.Length) {
      if (a.elements.Length == 1) {
        return new RealVector(b.elements.Select(x => x * a.elements[0]));
      }

      if (b.elements.Length == 1) {
        return new RealVector(a.elements.Select(x => x * b.elements[0]));
      }

      throw new ArgumentException("Vectors must be of the same length or one of length one");
    }

    var result = new double[a.elements.Length];
    for (var i = 0; i < a.elements.Length; i++) {
      result[i] = a.elements[i] * b.elements[i];
    }

    return new RealVector(result);
  }

  public static RealVector Divide(RealVector a, RealVector b)
  {
    if (a.elements.Length != b.elements.Length) {
      if (a.elements.Length == 1) {
        return new RealVector(b.elements.Select(x => x / a.elements[0]));
      }

      if (b.elements.Length == 1) {
        return new RealVector(a.elements.Select(x => x / b.elements[0]));
      }

      throw new ArgumentException("Vectors must be of the same length or one of length one");
    }

    var result = new double[a.elements.Length];
    for (var i = 0; i < a.elements.Length; i++) {
      result[i] = a.elements[i] / b.elements[i];
    }

    return new RealVector(result);
  }

  public static RealVector operator +(RealVector a, RealVector b) => Add(a, b);
  public static RealVector operator -(RealVector a, RealVector b) => Subtract(a, b);
  public static RealVector operator *(RealVector a, RealVector b) => Multiply(a, b);
  public static RealVector operator /(RealVector a, RealVector b) => Divide(a, b);

  public int Count => elements.Length;

  public bool Contains(double value) => elements.Contains(value);

  public bool Equals(RealVector? other)
  {
    if (other is null) {
      return false;
    }

    return ReferenceEquals(this, other)
           || elements.SequenceEqual(other.elements);
  }

  public override bool Equals(object? obj)
  {
    if (obj is null) {
      return false;
    }

    if (ReferenceEquals(this, obj)) {
      return true;
    }

    return obj is Permutation other && Equals(other);
  }

  public override int GetHashCode()
  {
    var hash = new HashCode();
    foreach (var element in elements) {
      hash.Add(element);
    }

    return hash.ToHashCode();
  }

  public static bool AreCompatible(RealVector a, RealVector b) => a.Count == b.Count || a.Count == 1 || b.Count == 1;

  public static bool AreCompatible(RealVector vector, params IEnumerable<RealVector> others) => others.All(v => AreCompatible(vector, v));

  public static bool AreCompatible(int length, params IEnumerable<RealVector> vectors) => vectors.All(v => v.Count == length || v.Count == 1);

  public static int BroadcastLength(RealVector a, RealVector b) => Math.Max(a.Count, b.Count);

  public static int BroadcastLength(RealVector vector, IEnumerable<RealVector> others)
  {
    if (!AreCompatible(vector, others)) {
      throw new ArgumentException("Vectors must be compatible for broadcasting");
    }

    return others.Max(v => v.Count);
  }

  public static RealVector CreateNormal(int length, RealVector mean, RealVector std, IRandomNumberGenerator random)
  {
    if (!AreCompatible(length, mean, std)) {
      throw new ArgumentException("Vectors must be compatible for broadcasting");
    }

    // Box-Muller transform to generate normal distributed random values
    RealVector u1, u2;
    do {
      u1 = new RealVector(random.NextDoubles(length));
    } while ((u1 <= 0.0).Any());

    u2 = new RealVector(random.NextDoubles(length));

    var mag = std * Sqrt(-2.0 * Log(u1));
    //var z0 = mag * Cos(2.0 * Math.PI * u2) + mean;
    var z1 = (mag * Sin(2.0 * Math.PI * u2)) + mean;

    return z1;
  }

  public static RealVector CreateUniform(int length, RealVector low, RealVector high, IRandomNumberGenerator random)
  {
    if (!AreCompatible(length, low, high)) {
      throw new ArgumentException("Vectors must be compatible for broadcasting");
    }

    var value = new RealVector(random.NextDoubles(length));
    value = low + ((high - low) * value);
    return value;
  }

  public static RealVector Sqrt(RealVector vector) => new(vector.Select(Math.Sqrt));

  public static RealVector Log(RealVector vector) => new(vector.Select(v => Math.Log(v)));

  public static RealVector Sin(RealVector vector) => new(vector.Select(Math.Sin));

  public static RealVector Clamp(RealVector input, RealVector? min, RealVector? max)
  {
    if (min is null && max is null) {
      return input;
    }

    var n = input.Count;
    var values = input.elements;

    var minIsScalar = min is null || min.Count <= 1;
    var maxIsScalar = max is null || max.Count <= 1;

    // Length validation
    if (!minIsScalar && min!.Count != n) {
      throw new ArgumentException($"Min vector must be of length 1 or match input length ({n})");
    }

    if (!maxIsScalar && max!.Count != n) {
      throw new ArgumentException($"Max vector must be of length 1 or match input length ({n})");
    }

    var minVals = min?.elements;
    var maxVals = max?.elements;

    var minScalarVal = min?.Count == 1 ? min[0] : double.NegativeInfinity;
    var maxScalarVal = max?.Count == 1 ? max[0] : double.PositiveInfinity;

    double[]? result = null;

    for (var i = 0; i < n; i++) {
      var v = values[i];
      var lo = minIsScalar ? minScalarVal : minVals![i];
      var hi = maxIsScalar ? maxScalarVal : maxVals![i];

      var clamped = v;
      var changed = false;

      if (v < lo) {
        clamped = lo;
        changed = true;
      }

      if (clamped > hi) {
        clamped = hi;
        changed = true;
      }

      if (result is not null) {
        result[i] = clamped;
      } else if (changed) {
        // First actual clamp -> allocate and copy prefix
        result = new double[n];
        Array.Copy(values, 0, result, 0, i);
        result[i] = clamped;
      }
    }

    return result is null ? input : new RealVector(result);
  }

  public static BoolVector operator >(RealVector a, RealVector b)
  {
    if (!AreCompatible(a, b)) {
      throw new ArgumentException("Vectors must be compatible for comparison");
    }

    var length = BroadcastLength(a, b);
    var result = new bool[length];

    for (var i = 0; i < length; i++) {
      var aValue = a.Count == 1 ? a[0] : a[i];
      var bValue = b.Count == 1 ? b[0] : b[i];
      result[i] = aValue > bValue;
    }

    return new BoolVector(result);
  }

  public static BoolVector operator <(RealVector a, RealVector b)
  {
    if (!AreCompatible(a, b)) {
      throw new ArgumentException("Vectors must be compatible for comparison");
    }

    var length = BroadcastLength(a, b);
    var result = new bool[length];

    for (var i = 0; i < length; i++) {
      var aValue = a.Count == 1 ? a[0] : a[i];
      var bValue = b.Count == 1 ? b[0] : b[i];
      result[i] = aValue < bValue;
    }

    return new BoolVector(result);
  }

  public static BoolVector operator >=(RealVector a, RealVector b)
  {
    if (!AreCompatible(a, b)) {
      throw new ArgumentException("Vectors must be compatible for comparison");
    }

    var length = BroadcastLength(a, b);
    var result = new bool[length];

    for (var i = 0; i < length; i++) {
      var aValue = a.Count == 1 ? a[0] : a[i];
      var bValue = b.Count == 1 ? b[0] : b[i];
      result[i] = aValue >= bValue;
    }

    return new BoolVector(result);
  }

  public static BoolVector operator <=(RealVector a, RealVector b)
  {
    if (!AreCompatible(a, b)) {
      throw new ArgumentException("Vectors must be compatible for comparison");
    }

    var length = BroadcastLength(a, b);
    var result = new bool[length];

    for (var i = 0; i < length; i++) {
      var aValue = a.Count == 1 ? a[0] : a[i];
      var bValue = b.Count == 1 ? b[0] : b[i];
      result[i] = aValue <= bValue;
    }

    return new BoolVector(result);
  }

  public static bool operator ==(RealVector a, RealVector b) => a.Equals(b);
  public static bool operator !=(RealVector a, RealVector b) => !a.Equals(b);

  public static RealVector Repeat(double value, int count) => new(Enumerable.Repeat(value, count));

  // public override bool Equals(object? obj) {
  //   if (obj is RealVector other)
  //     return elements.SequenceEqual(other.elements);
  //   return false;
  // }

  public override string ToString() => $"[{string.Join(", ", elements)}]";

  public double Norm()
  {
    var sumSquares = 0.0;
    for (var i = 0; i < elements.Length; i++) {
      var d1 = elements[i];
      sumSquares += d1;
    }

    return Math.Sqrt(sumSquares);
  }
}
