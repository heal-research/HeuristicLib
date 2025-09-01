using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Genotypes;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Blocker Code Smell", "S3877:Exceptions should not be thrown from unexpected methods")]
public sealed class RealVector : IReadOnlyList<double>, IEquatable<RealVector> {
  private readonly double[] elements;

  public RealVector(params IEnumerable<double> elements) {
    this.elements = elements.ToArray();
  }

  // public RealVector(double value) {
  //   elements = [value];
  // }

  public static implicit operator RealVector(double value) => new RealVector(value);

  public static implicit operator RealVector(double[] values) => new RealVector(values);
  //public static implicit operator RealVector?(double[]? values) => values is not null ? new RealVector(values) : null;

  //public static implicit operator RealVector(IntegerVector intVector) => new RealVector(intVector);

  public double this[int index] => elements[index];

  public double this[Index index] => elements[index];

  public IEnumerator<double> GetEnumerator() => ((IEnumerable<double>)elements).GetEnumerator();

  System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => elements.GetEnumerator();

  public static RealVector Add(RealVector a, RealVector b) {
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
    for (int i = 0; i < a.elements.Length; i++) {
      result[i] = a.elements[i] + b.elements[i];
    }

    return new RealVector(result);
  }

  public static RealVector Subtract(RealVector a, RealVector b) {
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
    for (int i = 0; i < a.elements.Length; i++) {
      result[i] = a.elements[i] - b.elements[i];
    }

    return new RealVector(result);
  }

  public static RealVector Multiply(RealVector a, RealVector b) {
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
    for (int i = 0; i < a.elements.Length; i++) {
      result[i] = a.elements[i] * b.elements[i];
    }

    return new RealVector(result);
  }

  public static RealVector Divide(RealVector a, RealVector b) {
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
    for (int i = 0; i < a.elements.Length; i++) {
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

  public bool Equals(RealVector? other) {
    if (other is null) return false;
    return ReferenceEquals(this, other)
           || elements.SequenceEqual(other.elements);
  }

  public override bool Equals(object? obj) {
    if (obj is null) return false;
    if (ReferenceEquals(this, obj)) return true;
    return obj is Permutation other && Equals(other);
  }

  public override int GetHashCode() {
    var hash = new HashCode();
    foreach (var element in elements) {
      hash.Add(element);
    }

    return hash.ToHashCode();
  }

  public static bool AreCompatible(RealVector a, RealVector b) {
    return a.Count == b.Count || a.Count == 1 || b.Count == 1;
  }

  public static bool AreCompatible(RealVector vector, params IEnumerable<RealVector> others) {
    return others.All(v => AreCompatible(vector, v));
  }

  public static bool AreCompatible(int length, params IEnumerable<RealVector> vectors) {
    return vectors.All(v => v.Count == length || v.Count == 1);
  }

  public static int BroadcastLength(RealVector a, RealVector b) {
    return Math.Max(a.Count, b.Count);
  }

  public static int BroadcastLength(RealVector vector, IEnumerable<RealVector> others) {
    if (!AreCompatible(vector, others)) throw new ArgumentException("Vectors must be compatible for broadcasting");
    return others.Max(v => v.Count);
  }

  public static RealVector CreateNormal(int length, RealVector mean, RealVector std, IRandomNumberGenerator random) {
    if (!AreCompatible(length, mean, std)) throw new ArgumentException("Vectors must be compatible for broadcasting");

    // Box-Muller transform to generate normal distributed random values
    RealVector u1, u2;
    do {
      u1 = new RealVector(random.Random(length));
    } while ((u1 <= 0.0).Any());

    u2 = new RealVector(random.Random(length));

    var mag = std * Sqrt(-2.0 * Log(u1));
    //var z0 = mag * Cos(2.0 * Math.PI * u2) + mean;
    var z1 = mag * Sin(2.0 * Math.PI * u2) + mean;

    return z1;
  }

  public static RealVector CreateUniform(int length, RealVector low, RealVector high, IRandomNumberGenerator random) {
    if (!AreCompatible(length, low, high)) throw new ArgumentException("Vectors must be compatible for broadcasting");

    RealVector value = new RealVector(random.Random(length));
    value = low + (high - low) * value;
    return value;
  }

  public static RealVector Sqrt(RealVector vector) {
    return new RealVector(vector.Select(Math.Sqrt));
  }

  public static RealVector Log(RealVector vector) {
    return new RealVector(vector.Select(v => Math.Log(v)));
  }

  public static RealVector Sin(RealVector vector) {
    return new RealVector(vector.Select(Math.Sin));
  }

  public static RealVector Clamp(RealVector input, RealVector? min, RealVector? max) {
    if (min is null && max is null) return input; // No clamping needed

    // Validate lengths
    if (min is not null && min.Count != 1 && min.Count != input.Count)
      throw new ArgumentException($"Min vector must be of length 1 or match input length ({input.Count})");

    if (max is not null && max.Count != 1 && max.Count != input.Count)
      throw new ArgumentException($"Max vector must be of length 1 or match input length ({input.Count})");

    double[] result = new double[input.Count];

    for (int i = 0; i < input.Count; i++) {
      double value = input[i];

      // Apply lower bound if present
      if (min is not null) {
        double minValue = min.Count == 1 ? min[0] : min[i];
        value = Math.Max(value, minValue);
      }

      // Apply upper bound if present
      if (max is not null) {
        double maxValue = max.Count == 1 ? max[0] : max[i];
        value = Math.Min(value, maxValue);
      }

      result[i] = value;
    }

    return new RealVector(result);
  }

  public static BoolVector operator >(RealVector a, RealVector b) {
    if (!AreCompatible(a, b)) throw new ArgumentException("Vectors must be compatible for comparison");

    int length = BroadcastLength(a, b);
    bool[] result = new bool[length];

    for (int i = 0; i < length; i++) {
      double aValue = a.Count == 1 ? a[0] : a[i];
      double bValue = b.Count == 1 ? b[0] : b[i];
      result[i] = aValue > bValue;
    }

    return new BoolVector(result);
  }

  public static BoolVector operator <(RealVector a, RealVector b) {
    if (!AreCompatible(a, b)) throw new ArgumentException("Vectors must be compatible for comparison");

    int length = BroadcastLength(a, b);
    bool[] result = new bool[length];

    for (int i = 0; i < length; i++) {
      double aValue = a.Count == 1 ? a[0] : a[i];
      double bValue = b.Count == 1 ? b[0] : b[i];
      result[i] = aValue < bValue;
    }

    return new BoolVector(result);
  }

  public static BoolVector operator >=(RealVector a, RealVector b) {
    if (!AreCompatible(a, b)) throw new ArgumentException("Vectors must be compatible for comparison");

    int length = BroadcastLength(a, b);
    bool[] result = new bool[length];

    for (int i = 0; i < length; i++) {
      double aValue = a.Count == 1 ? a[0] : a[i];
      double bValue = b.Count == 1 ? b[0] : b[i];
      result[i] = aValue >= bValue;
    }

    return new BoolVector(result);
  }

  public static BoolVector operator <=(RealVector a, RealVector b) {
    if (!AreCompatible(a, b)) throw new ArgumentException("Vectors must be compatible for comparison");

    int length = BroadcastLength(a, b);
    bool[] result = new bool[length];

    for (int i = 0; i < length; i++) {
      double aValue = a.Count == 1 ? a[0] : a[i];
      double bValue = b.Count == 1 ? b[0] : b[i];
      result[i] = aValue <= bValue;
    }

    return new BoolVector(result);
  }

  public static bool operator ==(RealVector a, RealVector b) => a.Equals(b);
  public static bool operator !=(RealVector a, RealVector b) => !a.Equals(b);

  public static RealVector Repeat(double value, int count) {
    return new RealVector(Enumerable.Repeat(value, count));
  }

  // public override bool Equals(object? obj) {
  //   if (obj is RealVector other)
  //     return elements.SequenceEqual(other.elements);
  //   return false;
  // }

  public override string ToString() {
    return $"[{string.Join(", ", elements)}]";
  }
}
