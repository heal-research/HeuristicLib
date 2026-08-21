using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Encodings.BoolVectors;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Encodings.Vectors;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Encodings.IntegerVectors;

[CollectionBuilder(typeof(IntegerVectorBuilder), nameof(IntegerVectorBuilder.Create))]
public sealed class IntegerVector : Vector<int>, IEquatable<IntegerVector>
{
    public IntegerVector(params ImmutableArray<int> elements)
        : base(elements)
    { }

    public IntegerVector(IEnumerable<int> elements)
        : base(elements)
    { }

    public bool Equals(IntegerVector? other) =>
      other is not null && (ReferenceEquals(this, other) || HasSameElements(other));

    public override bool Equals(object? obj) =>
      obj is IntegerVector other && Equals(other);

    public override int GetHashCode() => GetElementsHashCode();

    public RealVector ToRealVector() => ToRealVector(this);

    public double ToRealAt(int dimension) => ToRealAt(this, dimension);

    public IntegerVector Clamp(IntegerVector? min, IntegerVector? max) => Clamp(this, min, max);

    public int ClampAt(IntegerVector? min, IntegerVector? max, int dimension) => ClampAt(this, min, max, dimension);

    public static implicit operator IntegerVector(int value) => new(value);

    public static implicit operator RealVector(IntegerVector integerVector) => ToRealVector(integerVector);

    public static IntegerVector Create(params ImmutableArray<int> elements) => new(elements);

    public static IntegerVector Create(IEnumerable<int> elements) => new(elements);

    /// <summary>
    /// Creates a vector backed by <paramref name="elements"/> without copying it.
    /// The caller transfers ownership of the array and must not mutate it after this method returns.
    /// </summary>
    public static IntegerVector FromOwnedArray(int[] elements) => new(TakeOwnership(elements));

    public static IntegerVector CreateUniform(int length, IntegerVector low, IntegerVector high, IRandomNumberGenerator random)
      => random.NextIntegerVectorUniform(low, high, length);

    public static RealVector ToRealVector(IntegerVector input)
    {
        var result = new double[input.Count];
        for (var i = 0; i < input.Count; i++)
        {
            result[i] = input[i];
        }

        return RealVector.FromOwnedArray(result);
    }

    public static double ToRealAt(IntegerVector input, int dimension)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(dimension);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(dimension, input.Count);
        return input[dimension];
    }

    public static IntegerVector Add(IntegerVector a, IntegerVector b)
    {
        var length = BroadcastLength(a, b);
        var result = new int[length];
        for (var i = 0; i < length; i++)
        {
            var left = a.Count == 1 ? a[0] : a[i];
            var right = b.Count == 1 ? b[0] : b[i];
            result[i] = left + right;
        }

        return FromOwnedArray(result);
    }

    public static IntegerVector Subtract(IntegerVector a, IntegerVector b)
    {
        var length = BroadcastLength(a, b);
        var result = new int[length];
        for (var i = 0; i < length; i++)
        {
            var left = a.Count == 1 ? a[0] : a[i];
            var right = b.Count == 1 ? b[0] : b[i];
            result[i] = left - right;
        }

        return FromOwnedArray(result);
    }

    public static IntegerVector Multiply(IntegerVector a, IntegerVector b)
    {
        var length = BroadcastLength(a, b);
        var result = new int[length];
        for (var i = 0; i < length; i++)
        {
            var left = a.Count == 1 ? a[0] : a[i];
            var right = b.Count == 1 ? b[0] : b[i];
            result[i] = left * right;
        }

        return FromOwnedArray(result);
    }

    public static IntegerVector Divide(IntegerVector a, IntegerVector b)
    {
        var length = BroadcastLength(a, b);
        var result = new int[length];
        for (var i = 0; i < length; i++)
        {
            var left = a.Count == 1 ? a[0] : a[i];
            var right = b.Count == 1 ? b[0] : b[i];
            result[i] = left / right;
        }

        return FromOwnedArray(result);
    }

    public static IntegerVector Clamp(IntegerVector input, IntegerVector? min, IntegerVector? max)
    {
        if (min is null && max is null)
            return input;

        ValidateBounds(min, max, input.Count);

        var result = new int[input.Count];
        var changed = false;
        for (var i = 0; i < input.Count; i++)
        {
            result[i] = ClampAt(input[i], min, max, i);
            changed |= result[i] != input[i];
        }

        return changed ? FromOwnedArray(result) : input;
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

    public static IntegerVector operator +(IntegerVector a, IntegerVector b) => Add(a, b);

    public static IntegerVector operator -(IntegerVector a, IntegerVector b) => Subtract(a, b);

    public static IntegerVector operator *(IntegerVector a, IntegerVector b) => Multiply(a, b);

    public static IntegerVector operator /(IntegerVector a, IntegerVector b) => Divide(a, b);

    public static BoolVector operator >(IntegerVector a, IntegerVector b)
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

    public static BoolVector operator <(IntegerVector a, IntegerVector b)
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

    public static BoolVector operator >=(IntegerVector a, IntegerVector b)
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

    public static BoolVector operator <=(IntegerVector a, IntegerVector b)
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

    public static bool operator ==(IntegerVector? a, IntegerVector? b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (a is null || b is null)
        {
            return false;
        }

        return a.Equals(b);
    }

    public static bool operator !=(IntegerVector? a, IntegerVector? b) => !(a == b);

    private static void ValidateBounds(IntegerVector? minimum, IntegerVector? maximum, int? length = null, int? dimension = null)
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
}
