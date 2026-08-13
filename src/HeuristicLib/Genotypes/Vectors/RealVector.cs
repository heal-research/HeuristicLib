using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Genotypes.Vectors;

[CollectionBuilder(typeof(RealVectorBuilder), nameof(RealVectorBuilder.Create))]
[SuppressMessage("Blocker Code Smell", "S3877:Exceptions should not be thrown from unexpected methods")]
public sealed class RealVector : Vector<double>, IEquatable<RealVector>
{
    private const double IntegerBoundaryTolerance = 1e-12;

    public RealVector(params ImmutableArray<double> elements)
        : base(elements) { }

    public RealVector(IEnumerable<double> elements)
        : base(elements) { }

    // public static implicit operator RealVector(IntegerVector intVector) => new RealVector(intVector);

    public bool Equals(RealVector? other)
    {
        if (other is null)
            return false;

        return ReferenceEquals(this, other) || HasSameElements(other);
    }

    public override bool Equals(object? obj) => obj is RealVector other && Equals(other);

    public override int GetHashCode() => GetElementsHashCode();

    public double Dot(RealVector other)
    {
        var sum = 0.0;
        for (var i = 0; i < Elements.Length; i++)
        {
            var d1 = Elements[i];
            var d2 = other.Elements[i];
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
        for (var i = 0; i < Elements.Length; i++)
        {
            var d1 = Elements[i];
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
        var iElements = new int[Elements.Length];
        for (int i = 0; i < Elements.Length; i++)
        {
            iElements[i] = (int)Math.Round(Elements[i]);
        }

        return IntegerVector.FromOwnedArray(iElements);
    }

    public static implicit operator RealVector(double value) => new(value);

    public static RealVector Create(params ImmutableArray<double> elements) => new(elements);

    public static RealVector Create(IEnumerable<double> elements) => new(elements);

    /// <summary>
    /// Creates a vector backed by <paramref name="elements"/> without copying it.
    /// The caller transfers ownership of the array and must not mutate it after this method returns.
    /// </summary>
    public static RealVector FromOwnedArray(double[] elements) => new(TakeOwnership(elements));

    public static RealVector Repeat(double value, int count)
    {
        var elements = new double[count];
        Array.Fill(elements, value);
        return FromOwnedArray(elements);
    }

    public static RealVector CreateNormal(int length, RealVector mean, RealVector std, IRandomNumberGenerator random)
      => random.NextRealVectorNormal(mean, std, length);

    public static RealVector CreateUniform(int length, RealVector low, RealVector high, IRandomNumberGenerator random)
      => random.NextRealVectorUniform(low, high, length);

    public static RealVector Add(RealVector a, RealVector b)
    {
        var length = BroadcastLength(a, b);
        var result = new double[length];
        for (var i = 0; i < length; i++)
        {
            var left = a.Count == 1 ? a[0] : a[i];
            var right = b.Count == 1 ? b[0] : b[i];
            result[i] = left + right;
        }

        return FromOwnedArray(result);
    }

    public static RealVector Subtract(RealVector a, RealVector b)
    {
        var length = BroadcastLength(a, b);
        var result = new double[length];
        for (var i = 0; i < length; i++)
        {
            var left = a.Count == 1 ? a[0] : a[i];
            var right = b.Count == 1 ? b[0] : b[i];
            result[i] = left - right;
        }

        return FromOwnedArray(result);
    }

    public static RealVector Multiply(RealVector a, RealVector b)
    {
        var length = BroadcastLength(a, b);
        var result = new double[length];
        for (var i = 0; i < length; i++)
        {
            var left = a.Count == 1 ? a[0] : a[i];
            var right = b.Count == 1 ? b[0] : b[i];
            result[i] = left * right;
        }

        return FromOwnedArray(result);
    }

    public static RealVector Divide(RealVector a, RealVector b)
    {
        var length = BroadcastLength(a, b);
        var result = new double[length];
        for (var i = 0; i < length; i++)
        {
            var left = a.Count == 1 ? a[0] : a[i];
            var right = b.Count == 1 ? b[0] : b[i];
            result[i] = left / right;
        }

        return FromOwnedArray(result);
    }

    public static RealVector operator +(RealVector a, RealVector b) => Add(a, b);
    public static RealVector operator -(RealVector a, RealVector b) => Subtract(a, b);
    public static RealVector operator *(RealVector a, RealVector b) => Multiply(a, b);
    public static RealVector operator /(RealVector a, RealVector b) => Divide(a, b);

    public static RealVector Sqrt(RealVector vector) => new(vector.Select(Math.Sqrt));

    public static RealVector Log(RealVector vector) => new(vector.Select(v => Math.Log(v)));

    public static RealVector Sin(RealVector vector) => new(vector.Select(Math.Sin));

    public static RealVector Clamp(RealVector input, RealVector? min, RealVector? max)
    {
        if (min is null && max is null)
        {
            return input;
        }

        var n = input.Count;
        var values = input.Elements;

        var minIsScalar = min is null || min.Count == 1;
        var maxIsScalar = max is null || max.Count == 1;

        if (min is not null && max is not null)
        {
            if (!AreBroadcastableTo(n, min, max))
                throw new ArgumentException($"Bounds must be of length 1 or match input length ({n}).");
        }
        else if (min is not null && !AreBroadcastableTo(n, min))
        {
            throw new ArgumentException($"Min vector must be of length 1 or match input length ({n}).", nameof(min));
        }
        else if (max is not null && !AreBroadcastableTo(n, max))
        {
            throw new ArgumentException($"Max vector must be of length 1 or match input length ({n}).", nameof(max));
        }

        var minScalarVal = min?.Count == 1 ? min[0] : double.NegativeInfinity;
        var maxScalarVal = max?.Count == 1 ? max[0] : double.PositiveInfinity;

        double[]? result = null;

        for (var i = 0; i < n; i++)
        {
            var v = values[i];
            var lo = minIsScalar ? minScalarVal : min![i];
            var hi = maxIsScalar ? maxScalarVal : max![i];

            var clamped = v;
            var changed = false;

            if (v < lo)
            {
                clamped = lo;
                changed = true;
            }

            if (clamped > hi)
            {
                clamped = hi;
                changed = true;
            }

            if (result is not null)
            {
                result[i] = clamped;
            }
            else if (changed)
            {
                result = new double[n];
                values.AsSpan(0, i).CopyTo(result);
                result[i] = clamped;
            }
        }

        return result is null ? input : FromOwnedArray(result);
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
        for (var i = 0; i < input.Count; i++)
        {
            result[i] = FloorToIntegerAtUnchecked(input, minimum, maximum, i);
        }

        return IntegerVector.FromOwnedArray(result);
    }

    public static IntegerVector CeilToIntegerVector(RealVector input, IntegerVector minimum, IntegerVector maximum)
    {
        ValidateBounds(minimum, maximum, input.Count);

        var result = new int[input.Count];
        for (var i = 0; i < input.Count; i++)
        {
            result[i] = CeilToIntegerAtUnchecked(input, minimum, maximum, i);
        }

        return IntegerVector.FromOwnedArray(result);
    }

    public static IntegerVector RoundToIntegerVector(RealVector input, IntegerVector minimum, IntegerVector maximum)
    {
        ValidateBounds(minimum, maximum, input.Count);

        var result = new int[input.Count];
        for (var i = 0; i < input.Count; i++)
        {
            result[i] = RoundToIntegerAtUnchecked(input, minimum, maximum, i);
        }

        return IntegerVector.FromOwnedArray(result);
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
        if (double.IsNaN(value))
            return Math.Clamp(0, minimum, maximum);
        if (value <= minimum)
            return minimum;
        if (value >= maximum)
            return maximum;

        return (int)Math.Clamp(Math.Floor(value + IntegerBoundaryTolerance), minimum, maximum);
    }

    public static int CeilToInteger(double value, int minimum, int maximum)
    {
        if (double.IsNaN(value))
            return Math.Clamp(0, minimum, maximum);
        if (value <= minimum)
            return minimum;
        if (value >= maximum)
            return maximum;

        return (int)Math.Clamp(Math.Ceiling(value - IntegerBoundaryTolerance), minimum, maximum);
    }

    public static int RoundToInteger(double value, int minimum, int maximum)
    {
        if (double.IsNaN(value))
            return Math.Clamp(0, minimum, maximum);
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
        if (length is not null)
        {
            if (minimum is not null && maximum is not null)
            {
                if (!AreBroadcastableTo(length.Value, minimum, maximum))
                    throw new ArgumentException($"Bounds must be of length 1 or match input length ({length}).");
            }
            else if (minimum is not null && !AreBroadcastableTo(length.Value, minimum))
            {
                throw new ArgumentException($"Min vector must be of length 1 or match input length ({length}).", nameof(minimum));
            }
            else if (maximum is not null && !AreBroadcastableTo(length.Value, maximum))
            {
                throw new ArgumentException($"Max vector must be of length 1 or match input length ({length}).", nameof(maximum));
            }
        }

        if (dimension is not null)
        {
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

        var broadcastLength = dimension is not null ? 1 : BroadcastLength(minimum, maximum);
        for (var i = 0; i < broadcastLength; i++)
        {
            var index = dimension ?? i;
            var lower = minimum.Count == 1 ? minimum[0] : minimum[index];
            var upper = maximum.Count == 1 ? maximum[0] : maximum[index];
            if (lower > upper)
                throw new ArgumentException("Minimum values must be less than or equal to maximum values.");
        }
    }

    private static void ValidateBounds(IntegerVector minimum, IntegerVector maximum, int? length = null, int? dimension = null)
    {
        if (length is not null && !AreBroadcastableTo(length.Value, minimum, maximum))
            throw new ArgumentException("Bounds must be of length 1 or match input length.");

        if (dimension is not null)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(dimension.Value);
            if (length is not null)
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(dimension.Value, length.Value);
            if (minimum.Count != 1 && dimension >= minimum.Count)
                throw new ArgumentOutOfRangeException(nameof(dimension), "Dimension must be within the minimum bounds vector or minimum must be scalar.");
            if (maximum.Count != 1 && dimension >= maximum.Count)
                throw new ArgumentOutOfRangeException(nameof(dimension), "Dimension must be within the maximum bounds vector or maximum must be scalar.");
        }

        var broadcastLength = dimension is not null ? 1 : BroadcastLength(minimum, maximum);
        for (var i = 0; i < broadcastLength; i++)
        {
            var index = dimension ?? i;
            var lower = minimum.Count == 1 ? minimum[0] : minimum[index];
            var upper = maximum.Count == 1 ? maximum[0] : maximum[index];
            if (lower > upper)
                throw new ArgumentException("Minimum values must be less than or equal to maximum values.");
        }
    }

    public static BoolVector operator >(RealVector a, RealVector b)
    {
        var length = BroadcastLength(a, b);
        var result = new bool[length];

        for (var i = 0; i < length; i++)
        {
            var aValue = a.Count == 1 ? a[0] : a[i];
            var bValue = b.Count == 1 ? b[0] : b[i];
            result[i] = aValue > bValue;
        }

        return BoolVector.FromOwnedArray(result);
    }

    public static BoolVector operator <(RealVector a, RealVector b)
    {
        var length = BroadcastLength(a, b);
        var result = new bool[length];

        for (var i = 0; i < length; i++)
        {
            var aValue = a.Count == 1 ? a[0] : a[i];
            var bValue = b.Count == 1 ? b[0] : b[i];
            result[i] = aValue < bValue;
        }

        return BoolVector.FromOwnedArray(result);
    }

    public static BoolVector operator >=(RealVector a, RealVector b)
    {
        var length = BroadcastLength(a, b);
        var result = new bool[length];

        for (var i = 0; i < length; i++)
        {
            var aValue = a.Count == 1 ? a[0] : a[i];
            var bValue = b.Count == 1 ? b[0] : b[i];
            result[i] = aValue >= bValue;
        }

        return BoolVector.FromOwnedArray(result);
    }

    public static BoolVector operator <=(RealVector a, RealVector b)
    {
        var length = BroadcastLength(a, b);
        var result = new bool[length];

        for (var i = 0; i < length; i++)
        {
            var aValue = a.Count == 1 ? a[0] : a[i];
            var bValue = b.Count == 1 ? b[0] : b[i];
            result[i] = aValue <= bValue;
        }

        return BoolVector.FromOwnedArray(result);
    }

    public static bool operator ==(RealVector? a, RealVector? b) => Equals(a, b);
    public static bool operator !=(RealVector? a, RealVector? b) => !Equals(a, b);

    // public override bool Equals(object? obj) {
    //   if (obj is RealVector other)
    //     return elements.SequenceEqual(other.elements);
    //   return false;
    // }
}
