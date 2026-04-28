using System.Collections;
using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Genotypes.Vectors;

[SuppressMessage("Blocker Code Smell", "S3877:Exceptions should not be thrown from unexpected methods")]
public sealed class RealVector(params IEnumerable<double> elements) : IReadOnlyList<double>, IEquatable<RealVector>
{
  private readonly double[] elements = elements.ToArray();

  private const double IntegerBoundaryTolerance = 1e-12;

  public double this[Index index] => elements[index];

  // public static implicit operator RealVector?(double[]? values) => values is not null ? new RealVector(values) : null;

  // public static implicit operator RealVector(IntegerVector intVector) => new RealVector(intVector);

  public double this[int index] => elements[index];

  public int Count => elements.Length;

  public IEnumerator<double> GetEnumerator() => ((IEnumerable<double>)elements).GetEnumerator();

  IEnumerator IEnumerable.GetEnumerator() => elements.GetEnumerator();

  public bool Contains(double value) => elements.Contains(value);

  public bool Equals(RealVector? other)
  {
    if (other is null) {
      return false;
    }

    return ReferenceEquals(this, other)
           || elements.SequenceEqual(other.elements);
  }

  public override bool Equals(object? obj) => obj is RealVector other && Equals(other);

  public override int GetHashCode()
  {
    var hash = new HashCode();
    foreach (var element in elements) {
      hash.Add(element);
    }

    return hash.ToHashCode();
  }

  public override string ToString() => $"[{string.Join(", ", elements)}]";

  public double Dot(RealVector other)
  {
    var sum = 0.0;
    for (var i = 0; i < elements.Length; i++) {
      var d1 = elements[i];
      var d2 = other.elements[i];
      sum += d1 * d2;
    }

    return sum;
  }

  public double Angle(RealVector other)
  {
    var r = Dot(other) / (Norm() * other.Norm());
    r = Math.Clamp(r, -1.0, 1.0);
    return Math.Acos(r);
  }

  public double Norm()
  {
    var sumSquares = 0.0;
    for (var i = 0; i < elements.Length; i++) {
      var d1 = elements[i];
      sumSquares += d1 * d1;
    }

    return Math.Sqrt(sumSquares);
  }

  public RealVector Clamp(RealVector? min, RealVector? max) => Clamp(this, min, max);

  public double ClampAt(RealVector? min, RealVector? max, int dimension) => ClampAt(this, min, max, dimension);

  public RealVector Floor() => Floor(this);

  public double FloorAt(int dimension) => FloorAt(this, dimension);

  public RealVector Ceil() => Ceil(this);

  public double CeilAt(int dimension) => CeilAt(this, dimension);

  public RealVector Round() => Round(this);

  public double RoundAt(int dimension) => RoundAt(this, dimension);

  public IntegerVector FloorToIntegerVector(IntegerVector minimum, IntegerVector maximum)
    => FloorToIntegerVector(this, minimum, maximum);

  public IntegerVector CeilToIntegerVector(IntegerVector minimum, IntegerVector maximum)
    => CeilToIntegerVector(this, minimum, maximum);

  public IntegerVector RoundToIntegerVector(IntegerVector minimum, IntegerVector maximum)
    => RoundToIntegerVector(this, minimum, maximum);

  public int FloorToIntegerAt(IntegerVector minimum, IntegerVector maximum, int dimension)
    => FloorToIntegerAt(this, minimum, maximum, dimension);

  public int CeilToIntegerAt(IntegerVector minimum, IntegerVector maximum, int dimension)
    => CeilToIntegerAt(this, minimum, maximum, dimension);

  public int RoundToIntegerAt(IntegerVector minimum, IntegerVector maximum, int dimension)
    => RoundToIntegerAt(this, minimum, maximum, dimension);

  public IntegerVector AsIntegerVector()
  {
    var iElements = new int[elements.Length];
    for (int i = 0; i < elements.Length; i++) {
      iElements[i] = (int)Math.Round(elements[i]);
    }

    return iElements;
  }

  // public RealVector(double value) {
  //   elements = [value];
  // }

  public static implicit operator RealVector(double value) => new(value);

  public static implicit operator RealVector(double[] values) => new(values);

  public static RealVector Repeat(double value, int count) => new(Enumerable.Repeat(value, count));

  public static RealVector CreateNormal(int length, RealVector mean, RealVector std, IRandomNumberGenerator random)
    => random.NextRealVectorNormal(mean, std, length);

  public static RealVector CreateUniform(int length, RealVector low, RealVector high, IRandomNumberGenerator random)
    => random.NextRealVectorUniform(low, high, length);

  public static RealVector Add(RealVector a, RealVector b)
  {
    if (!AreCompatible(a, b))
      throw new ArgumentException("Vectors must be of the same length or one of length one");

    var length = BroadcastLength(a, b);
    var result = new double[length];
    for (var i = 0; i < length; i++) {
      var left = a.Count == 1 ? a[0] : a[i];
      var right = b.Count == 1 ? b[0] : b[i];
      result[i] = left + right;
    }

    return new RealVector(result);
  }

  public static RealVector Subtract(RealVector a, RealVector b)
  {
    if (!AreCompatible(a, b))
      throw new ArgumentException("Vectors must be of the same length or one of length one");

    var length = BroadcastLength(a, b);
    var result = new double[length];
    for (var i = 0; i < length; i++) {
      var left = a.Count == 1 ? a[0] : a[i];
      var right = b.Count == 1 ? b[0] : b[i];
      result[i] = left - right;
    }

    return new RealVector(result);
  }

  public static RealVector Multiply(RealVector a, RealVector b)
  {
    if (!AreCompatible(a, b))
      throw new ArgumentException("Vectors must be of the same length or one of length one");

    var length = BroadcastLength(a, b);
    var result = new double[length];
    for (var i = 0; i < length; i++) {
      var left = a.Count == 1 ? a[0] : a[i];
      var right = b.Count == 1 ? b[0] : b[i];
      result[i] = left * right;
    }

    return new RealVector(result);
  }

  public static RealVector Divide(RealVector a, RealVector b)
  {
    if (!AreCompatible(a, b))
      throw new ArgumentException("Vectors must be of the same length or one of length one");

    var length = BroadcastLength(a, b);
    var result = new double[length];
    for (var i = 0; i < length; i++) {
      var left = a.Count == 1 ? a[0] : a[i];
      var right = b.Count == 1 ? b[0] : b[i];
      result[i] = left / right;
    }

    return new RealVector(result);
  }

  public static RealVector operator +(RealVector a, RealVector b) => Add(a, b);
  public static RealVector operator -(RealVector a, RealVector b) => Subtract(a, b);
  public static RealVector operator *(RealVector a, RealVector b) => Multiply(a, b);
  public static RealVector operator /(RealVector a, RealVector b) => Divide(a, b);

  public static bool AreCompatible(RealVector a, RealVector b) => a.Count == b.Count || a.Count == 1 || b.Count == 1;

  public static bool AreCompatible(RealVector vector, params IEnumerable<RealVector> others) => others.All(v => AreCompatible(vector, v));

  public static bool AreCompatible(int length, params IEnumerable<RealVector> vectors) => vectors.All(v => v.Count == length || v.Count == 1);

  public static int BroadcastLength(RealVector a, RealVector b) => Math.Max(a.Count, b.Count);

  public static int BroadcastLength(RealVector vector, params IReadOnlyCollection<RealVector> others)
  {
    ArgumentNullException.ThrowIfNull(others);
    return !AreCompatible(vector, others) ? throw new ArgumentException("Vectors must be compatible for broadcasting") : others.Append(vector).Max(v => v.Count);
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
        result = new double[n];
        Array.Copy(values, 0, result, 0, i);
        result[i] = clamped;
      }
    }

    return result is null ? input : new RealVector(result);
  }

  public static double ClampAt(RealVector input, RealVector? min, RealVector? max, int dimension)
  {
    ValidateBounds(min, max, input.Count, dimension);

    return ClampAt(input[dimension], min, max, dimension);
  }

  public static double ClampAt(double value, RealVector? min, RealVector? max, int dimension)
  {
    ValidateBounds(min, max, dimension: dimension);

    var lower = double.NegativeInfinity;
    if (min is not null)
      lower = min.Count == 1 ? min[0] : min[dimension];

    var upper = double.PositiveInfinity;
    if (max is not null)
      upper = max.Count == 1 ? max[0] : max[dimension];

    return Math.Clamp(value, lower, upper);
  }

  public static RealVector Floor(RealVector input) => new(input.Select(Math.Floor));

  public static double FloorAt(RealVector input, int dimension)
  {
    ArgumentOutOfRangeException.ThrowIfNegative(dimension);
    ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(dimension, input.Count);
    return Math.Floor(input[dimension]);
  }

  public static RealVector Ceil(RealVector input) => new(input.Select(Math.Ceiling));

  public static double CeilAt(RealVector input, int dimension)
  {
    ArgumentOutOfRangeException.ThrowIfNegative(dimension);
    ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(dimension, input.Count);
    return Math.Ceiling(input[dimension]);
  }

  public static RealVector Round(RealVector input) => new(input.Select(v => Math.Round(v, MidpointRounding.AwayFromZero)));

  public static double RoundAt(RealVector input, int dimension)
  {
    ArgumentOutOfRangeException.ThrowIfNegative(dimension);
    ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(dimension, input.Count);
    return Math.Round(input[dimension], MidpointRounding.AwayFromZero);
  }

  public static IntegerVector FloorToIntegerVector(RealVector input, IntegerVector minimum, IntegerVector maximum)
  {
    ValidateBounds(minimum, maximum, input.Count);

    var result = new int[input.Count];
    for (var i = 0; i < input.Count; i++) {
      result[i] = FloorToIntegerAtUnchecked(input, minimum, maximum, i);
    }

    return result;
  }

  public static IntegerVector CeilToIntegerVector(RealVector input, IntegerVector minimum, IntegerVector maximum)
  {
    ValidateBounds(minimum, maximum, input.Count);

    var result = new int[input.Count];
    for (var i = 0; i < input.Count; i++) {
      result[i] = CeilToIntegerAtUnchecked(input, minimum, maximum, i);
    }

    return result;
  }

  public static IntegerVector RoundToIntegerVector(RealVector input, IntegerVector minimum, IntegerVector maximum)
  {
    ValidateBounds(minimum, maximum, input.Count);

    var result = new int[input.Count];
    for (var i = 0; i < input.Count; i++) {
      result[i] = RoundToIntegerAtUnchecked(input, minimum, maximum, i);
    }

    return result;
  }

  public static int FloorToIntegerAt(double value, IntegerVector minimum, IntegerVector maximum, int dimension)
  {
    ValidateBounds(minimum, maximum, dimension: dimension);
    return FloorToIntegerAtUnchecked(value, minimum, maximum, dimension);
  }

  public static int FloorToIntegerAt(RealVector input, IntegerVector minimum, IntegerVector maximum, int dimension)
  {
    ValidateBounds(minimum, maximum, input.Count, dimension);

    return FloorToIntegerAtUnchecked(input, minimum, maximum, dimension);
  }

  public static int CeilToIntegerAt(double value, IntegerVector minimum, IntegerVector maximum, int dimension)
  {
    ValidateBounds(minimum, maximum, dimension: dimension);
    return CeilToIntegerAtUnchecked(value, minimum, maximum, dimension);
  }

  public static int CeilToIntegerAt(RealVector input, IntegerVector minimum, IntegerVector maximum, int dimension)
  {
    ValidateBounds(minimum, maximum, input.Count, dimension);

    return CeilToIntegerAtUnchecked(input, minimum, maximum, dimension);
  }

  public static int RoundToIntegerAt(double value, IntegerVector minimum, IntegerVector maximum, int dimension)
  {
    ValidateBounds(minimum, maximum, dimension: dimension);
    return RoundToIntegerAtUnchecked(value, minimum, maximum, dimension);
  }

  public static int RoundToIntegerAt(RealVector input, IntegerVector minimum, IntegerVector maximum, int dimension)
  {
    ValidateBounds(minimum, maximum, input.Count, dimension);

    return RoundToIntegerAtUnchecked(input, minimum, maximum, dimension);
  }

  public static int FloorToInteger(double value, int minimum, int maximum)
  {
    if (value <= minimum)
      return minimum;
    if (value >= maximum)
      return maximum;

    return (int)Math.Clamp(Math.Floor(value + IntegerBoundaryTolerance), minimum, maximum);
  }

  public static int CeilToInteger(double value, int minimum, int maximum)
  {
    if (value <= minimum)
      return minimum;
    if (value >= maximum)
      return maximum;

    return (int)Math.Clamp(Math.Ceiling(value - IntegerBoundaryTolerance), minimum, maximum);
  }

  public static int RoundToInteger(double value, int minimum, int maximum)
  {
    if (value <= minimum)
      return minimum;
    if (value >= maximum)
      return maximum;

    return (int)Math.Clamp(Math.Round(value, MidpointRounding.AwayFromZero), minimum, maximum);
  }

  private static int FloorToIntegerAtUnchecked(double value, IntegerVector minimum, IntegerVector maximum, int dimension)
    => FloorToInteger(value, minimum.Count == 1 ? minimum[0] : minimum[dimension], maximum.Count == 1 ? maximum[0] : maximum[dimension]);

  private static int FloorToIntegerAtUnchecked(RealVector input, IntegerVector minimum, IntegerVector maximum, int dimension)
    => FloorToIntegerAtUnchecked(input[dimension], minimum, maximum, dimension);

  private static int CeilToIntegerAtUnchecked(double value, IntegerVector minimum, IntegerVector maximum, int dimension)
    => CeilToInteger(value, minimum.Count == 1 ? minimum[0] : minimum[dimension], maximum.Count == 1 ? maximum[0] : maximum[dimension]);

  private static int CeilToIntegerAtUnchecked(RealVector input, IntegerVector minimum, IntegerVector maximum, int dimension)
    => CeilToIntegerAtUnchecked(input[dimension], minimum, maximum, dimension);

  private static int RoundToIntegerAtUnchecked(double value, IntegerVector minimum, IntegerVector maximum, int dimension)
    => RoundToInteger(value, minimum.Count == 1 ? minimum[0] : minimum[dimension], maximum.Count == 1 ? maximum[0] : maximum[dimension]);

  private static int RoundToIntegerAtUnchecked(RealVector input, IntegerVector minimum, IntegerVector maximum, int dimension)
    => RoundToIntegerAtUnchecked(input[dimension], minimum, maximum, dimension);

  private static void ValidateBounds(RealVector? minimum, RealVector? maximum, int? length = null, int? dimension = null)
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

  private static void ValidateBounds(IntegerVector minimum, IntegerVector maximum, int? length = null, int? dimension = null)
  {
    if (length is not null) {
      if (minimum.Count != 1 && minimum.Count != length)
        throw new ArgumentException("Minimum vector must be of length 1 or match input length.", nameof(minimum));
      if (maximum.Count != 1 && maximum.Count != length)
        throw new ArgumentException("Maximum vector must be of length 1 or match input length.", nameof(maximum));
    }

    if (dimension is not null) {
      ArgumentOutOfRangeException.ThrowIfNegative(dimension.Value);
      if (length is not null)
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(dimension.Value, length.Value);
      if (minimum.Count != 1 && dimension >= minimum.Count)
        throw new ArgumentOutOfRangeException(nameof(dimension), "Dimension must be within the minimum bounds vector or minimum must be scalar.");
      if (maximum.Count != 1 && dimension >= maximum.Count)
        throw new ArgumentOutOfRangeException(nameof(dimension), "Dimension must be within the maximum bounds vector or maximum must be scalar.");
    }

    var broadcastLength = dimension is not null ? 1 : Math.Max(minimum.Count, maximum.Count);
    for (var i = 0; i < broadcastLength; i++) {
      var index = dimension ?? i;
      var lower = minimum.Count == 1 ? minimum[0] : minimum[index];
      var upper = maximum.Count == 1 ? maximum[0] : maximum[index];
      if (lower > upper)
        throw new ArgumentException("Minimum values must be less than or equal to maximum values.");
    }
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

  public static bool operator ==(RealVector? a, RealVector? b) => Equals(a, b);
  public static bool operator !=(RealVector? a, RealVector? b) => !Equals(a, b);

  // public override bool Equals(object? obj) {
  //   if (obj is RealVector other)
  //     return elements.SequenceEqual(other.elements);
  //   return false;
  // }
}
