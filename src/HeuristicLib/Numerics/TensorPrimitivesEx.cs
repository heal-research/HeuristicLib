using System.Numerics;
using System.Numerics.Tensors;

namespace HEAL.HeuristicLib.Numerics;

/// <summary>
/// The element-wise shapes <see cref="TensorPrimitives"/> does not provide.
/// </summary>
/// <remarks>
/// Two gaps are filled here. <see cref="TensorPrimitives"/> solves span-or-single-value with an overload per shape,
/// which an <see cref="Operand"/> cannot use because it carries its shape at run time; these dispatch on it once for
/// the whole span rather than once per element. And it has no accumulating divide to match its
/// <see cref="TensorPrimitives.MultiplyAdd(ReadOnlySpan{double}, ReadOnlySpan{double}, ReadOnlySpan{double}, Span{double})"/>.
/// Everything else a caller needs is already there and should be called there.
/// </remarks>
internal static class TensorPrimitivesEx
{
    /// <summary><c>destination = log(x)</c>.</summary>
    internal static void Log(Operand x, Span<double> destination)
    {
        if (x.IsScalar)
            destination.Fill(Math.Log(x.Scalar));
        else
            TensorPrimitives.Log(x.Span, destination);
    }

    /// <summary><c>destination = 1 / x</c>.</summary>
    internal static void Reciprocal(Operand x, Span<double> destination)
    {
        if (x.IsScalar)
            destination.Fill(1.0 / x.Scalar);
        else
            TensorPrimitives.Reciprocal(x.Span, destination);
    }

    /// <summary><c>destination = x * x</c>.</summary>
    internal static void Square(Operand x, Span<double> destination)
    {
        if (x.IsScalar)
            destination.Fill(x.Scalar * x.Scalar);
        else
            TensorPrimitives.Multiply(x.Span, x.Span, destination);
    }

    /// <summary><c>destination = x - y</c>.</summary>
    internal static void Subtract(Operand x, double y, Span<double> destination)
    {
        if (x.IsScalar)
            destination.Fill(x.Scalar - y);
        else
            TensorPrimitives.Subtract(x.Span, y, destination);
    }

    /// <summary><c>destination = x * y</c>.</summary>
    internal static void Multiply(ReadOnlySpan<double> x, Operand y, Span<double> destination)
    {
        if (y.IsScalar)
            TensorPrimitives.Multiply(x, y.Scalar, destination);
        else
            TensorPrimitives.Multiply(x, y.Span, destination);
    }

    /// <summary><c>destination = x raised to y</c>.</summary>
    internal static void Pow(Operand x, ReadOnlySpan<double> y, Span<double> destination)
    {
        if (x.IsScalar)
            TensorPrimitives.Pow(x.Scalar, y, destination);
        else
            TensorPrimitives.Pow(x.Span, y, destination);
    }

    /// <summary><c>destination = (x * y) + addend</c>.</summary>
    internal static void MultiplyAdd(ReadOnlySpan<double> x, Operand y, ReadOnlySpan<double> addend, Span<double> destination)
    {
        if (y.IsScalar)
            TensorPrimitives.MultiplyAdd(x, y.Scalar, addend, destination);
        else
            TensorPrimitives.MultiplyAdd(x, y.Span, addend, destination);
    }

    /// <summary><c>destination = (x / y) + addend</c>.</summary>
    /// <remarks>A single value becomes one reciprocal and a multiply; a span has to be divided element by element.</remarks>
    internal static void DivideAdd(ReadOnlySpan<double> x, Operand y, ReadOnlySpan<double> addend, Span<double> destination)
    {
        if (y.IsScalar)
        {
            TensorPrimitives.MultiplyAdd(x, 1.0 / y.Scalar, addend, destination);
            return;
        }

        var elementIndex = 0;
        var vectorizedElementCount = VectorizedCount(x.Length);
        for (; elementIndex < vectorizedElementCount; elementIndex += Vector<double>.Count)
        {
            var left = new Vector<double>(x.Slice(elementIndex));
            var right = new Vector<double>(y.Span.Slice(elementIndex));
            var current = new Vector<double>(addend.Slice(elementIndex));
            (current + (left / right)).CopyTo(destination.Slice(elementIndex));
        }

        for (; elementIndex < x.Length; elementIndex++)
        {
            destination[elementIndex] = addend[elementIndex] + (x[elementIndex] / y.Span[elementIndex]);
        }
    }

    /// <summary>How many leading elements a <see cref="Vector{T}"/> loop covers, leaving the rest to a scalar tail.</summary>
    internal static int VectorizedCount(int count) => count - (count % Vector<double>.Count);
}
