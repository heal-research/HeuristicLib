using System.Numerics;
using System.Numerics.Tensors;

namespace HEAL.HeuristicLib.Numerics;

/// <summary>
/// The built-in operations, in arity order: terminals, then unary, then binary. The set is closed: a consumer needing
/// another operation defines a symbol whose emission expands into these, which keeps it evaluable, differentiable and
/// portable by the same machinery.
/// </summary>
/// <remarks>
/// A terminal reads a value rather than computing one, so it has no kernels and no adjoint rule. It is where a
/// reverse sweep stops, not a step it takes: a variable and a constant contribute no derivative, and a parameter is
/// where the accumulated adjoints are read out.
/// </remarks>
public readonly struct VariableDefinition : ITerminalOperationDefinition
{
    public static Operation Operation => Operation.Variable;
    public static string Name => "variable";
    public static bool IsDifferentiable => true;
    public static PayloadKind PayloadKind => PayloadKind.VariableReference;
}

public readonly struct ConstantDefinition : ITerminalOperationDefinition
{
    public static Operation Operation => Operation.Constant;
    public static string Name => "constant";
    public static bool IsDifferentiable => true;
    public static PayloadKind PayloadKind => PayloadKind.Constant;
}

public readonly struct ParameterDefinition : ITerminalOperationDefinition
{
    public static Operation Operation => Operation.Parameter;
    public static string Name => "parameter";
    public static bool IsDifferentiable => true;
    public static PayloadKind PayloadKind => PayloadKind.Parameter;
}

public readonly struct NegateDefinition : IUnaryOperationDefinition
{
    public static Operation Operation => Operation.Negate;
    public static string Name => "negate";
    public static bool IsDifferentiable => true;
    public static int ScratchSpanCount => 0;
    public static int AdjointScratchSpanCount => 0;
    public static OperationNotation Notation => OperationNotation.Function;

    public static double Apply(double value) => -value;
    public static void Apply(ReadOnlySpan<double> value, Span<double> result, ScratchSpans scratch) => TensorPrimitives.Negate(value, result);

    public static void Adjoint(ReadOnlySpan<double> upstream, Operand operand, Operand result, Span<double> operandAdjoints, ScratchSpans scratch) =>
        TensorPrimitives.MultiplyAdd(upstream, -1.0, operandAdjoints, operandAdjoints);
}

public readonly struct ExpDefinition : IUnaryOperationDefinition
{
    public static Operation Operation => Operation.Exp;
    public static string Name => "exp";
    public static bool IsDifferentiable => true;
    public static int ScratchSpanCount => 0;
    public static int AdjointScratchSpanCount => 0;
    public static OperationNotation Notation => OperationNotation.Function;

    public static double Apply(double value) => Math.Exp(value);
    public static void Apply(ReadOnlySpan<double> value, Span<double> result, ScratchSpans scratch) => TensorPrimitives.Exp(value, result);

    /// <remarks>The derivative of <c>exp(x)</c> is <c>exp(x)</c>, so the retained result is the factor.</remarks>
    public static void Adjoint(ReadOnlySpan<double> upstream, Operand operand, Operand result, Span<double> operandAdjoints, ScratchSpans scratch) =>
        TensorPrimitivesEx.MultiplyAdd(upstream, result, operandAdjoints, operandAdjoints);
}

public readonly struct LogDefinition : IUnaryOperationDefinition
{
    public static Operation Operation => Operation.Log;
    public static string Name => "log";
    public static bool IsDifferentiable => true;
    public static int ScratchSpanCount => 0;
    public static int AdjointScratchSpanCount => 0;
    public static OperationNotation Notation => OperationNotation.Function;

    public static double Apply(double value) => Math.Log(value);
    public static void Apply(ReadOnlySpan<double> value, Span<double> result, ScratchSpans scratch) => TensorPrimitives.Log(value, result);

    /// <remarks>The derivative of <c>log(x)</c> is <c>1 / x</c>.</remarks>
    public static void Adjoint(ReadOnlySpan<double> upstream, Operand operand, Operand result, Span<double> operandAdjoints, ScratchSpans scratch) =>
        TensorPrimitivesEx.DivideAdd(upstream, operand, operandAdjoints, operandAdjoints);
}

public readonly struct SqrtDefinition : IUnaryOperationDefinition
{
    public static Operation Operation => Operation.Sqrt;
    public static string Name => "sqrt";
    public static bool IsDifferentiable => true;
    public static int ScratchSpanCount => 0;
    public static int AdjointScratchSpanCount => 0;
    public static OperationNotation Notation => OperationNotation.Function;

    public static double Apply(double value) => Math.Sqrt(value);
    public static void Apply(ReadOnlySpan<double> value, Span<double> result, ScratchSpans scratch) => TensorPrimitives.Sqrt(value, result);

    /// <remarks>
    /// The derivative of <c>sqrt(x)</c> is <c>1 / (2 * sqrt(x))</c>, which is <c>0.5 / result</c>, so the retained
    /// result is used rather than recomputing a root. At <c>x = 0</c> this divides by zero and yields an infinite
    /// derivative, which is the true one; ordinary IEEE propagation applies as everywhere else in the engine.
    /// </remarks>
    public static void Adjoint(ReadOnlySpan<double> upstream, Operand operand, Operand result, Span<double> operandAdjoints, ScratchSpans scratch)
    {
        if (result.IsScalar)
        {
            TensorPrimitives.MultiplyAdd(upstream, 0.5 / result.Scalar, operandAdjoints, operandAdjoints);
            return;
        }

        var elementIndex = 0;
        var vectorizedElementCount = TensorPrimitivesEx.VectorizedCount(upstream.Length);
        var half = new Vector<double>(0.5);
        for (; elementIndex < vectorizedElementCount; elementIndex += Vector<double>.Count)
        {
            var upstreamValues = new Vector<double>(upstream.Slice(elementIndex));
            var primal = new Vector<double>(result.Span.Slice(elementIndex));
            var adjoints = new Vector<double>(operandAdjoints.Slice(elementIndex));
            (adjoints + (upstreamValues * (half / primal))).CopyTo(operandAdjoints.Slice(elementIndex));
        }

        for (; elementIndex < upstream.Length; elementIndex++)
        {
            operandAdjoints[elementIndex] += upstream[elementIndex] * (0.5 / result.Span[elementIndex]);
        }
    }
}

public readonly struct AbsDefinition : IUnaryOperationDefinition
{
    public static Operation Operation => Operation.Abs;
    public static string Name => "abs";
    public static bool IsDifferentiable => true;
    public static int ScratchSpanCount => 0;
    public static int AdjointScratchSpanCount => 0;
    public static OperationNotation Notation => OperationNotation.Function;

    public static double Apply(double value) => Math.Abs(value);
    public static void Apply(ReadOnlySpan<double> value, Span<double> result, ScratchSpans scratch) => TensorPrimitives.Abs(value, result);

    /// <remarks>
    /// The derivative of <c>abs(x)</c> is <c>sign(x)</c>. It does not exist at <c>x = 0</c>, where this returns zero:
    /// that is the subgradient the established automatic-differentiation frameworks also choose, and it keeps the
    /// derivative finite where the one-sided limits disagree.
    /// </remarks>
    public static void Adjoint(ReadOnlySpan<double> upstream, Operand operand, Operand result, Span<double> operandAdjoints, ScratchSpans scratch)
    {
        if (operand.IsScalar)
        {
            TensorPrimitives.MultiplyAdd(upstream, Math.Sign(operand.Scalar), operandAdjoints, operandAdjoints);
            return;
        }

        for (var elementIndex = 0; elementIndex < upstream.Length; elementIndex++)
        {
            operandAdjoints[elementIndex] += upstream[elementIndex] * Math.Sign(operand.Span[elementIndex]);
        }
    }
}

public readonly struct SquareDefinition : IUnaryOperationDefinition
{
    public static Operation Operation => Operation.Square;
    public static string Name => "square";
    public static bool IsDifferentiable => true;
    public static int ScratchSpanCount => 0;
    public static int AdjointScratchSpanCount => 0;
    public static OperationNotation Notation => OperationNotation.Function;

    public static double Apply(double value) => value * value;
    public static void Apply(ReadOnlySpan<double> value, Span<double> result, ScratchSpans scratch) => TensorPrimitives.Multiply(value, value, result);

    /// <remarks>The derivative of <c>x * x</c> is <c>2x</c>.</remarks>
    public static void Adjoint(ReadOnlySpan<double> upstream, Operand operand, Operand result, Span<double> operandAdjoints, ScratchSpans scratch)
    {
        if (operand.IsScalar)
        {
            TensorPrimitives.MultiplyAdd(upstream, 2.0 * operand.Scalar, operandAdjoints, operandAdjoints);
            return;
        }

        var elementIndex = 0;
        var vectorizedElementCount = TensorPrimitivesEx.VectorizedCount(upstream.Length);
        var two = new Vector<double>(2.0);
        for (; elementIndex < vectorizedElementCount; elementIndex += Vector<double>.Count)
        {
            var upstreamValues = new Vector<double>(upstream.Slice(elementIndex));
            var primal = new Vector<double>(operand.Span.Slice(elementIndex));
            var adjoints = new Vector<double>(operandAdjoints.Slice(elementIndex));
            (adjoints + (upstreamValues * two * primal)).CopyTo(operandAdjoints.Slice(elementIndex));
        }

        for (; elementIndex < upstream.Length; elementIndex++)
        {
            operandAdjoints[elementIndex] += upstream[elementIndex] * 2.0 * operand.Span[elementIndex];
        }
    }
}

public readonly struct CubeDefinition : IUnaryOperationDefinition
{
    public static Operation Operation => Operation.Cube;
    public static string Name => "cube";
    public static bool IsDifferentiable => true;
    public static int ScratchSpanCount => 0;
    public static int AdjointScratchSpanCount => 0;
    public static OperationNotation Notation => OperationNotation.Function;

    public static double Apply(double value) => value * value * value;
    public static void Apply(ReadOnlySpan<double> value, Span<double> result, ScratchSpans scratch) => TensorPrimitives.Pow(value, 3.0, result);

    /// <remarks>The derivative of <c>x * x * x</c> is three times <c>x</c> squared.</remarks>
    public static void Adjoint(ReadOnlySpan<double> upstream, Operand operand, Operand result, Span<double> operandAdjoints, ScratchSpans scratch)
    {
        if (operand.IsScalar)
        {
            TensorPrimitives.MultiplyAdd(upstream, 3.0 * operand.Scalar * operand.Scalar, operandAdjoints, operandAdjoints);
            return;
        }

        var elementIndex = 0;
        var vectorizedElementCount = TensorPrimitivesEx.VectorizedCount(upstream.Length);
        var three = new Vector<double>(3.0);
        for (; elementIndex < vectorizedElementCount; elementIndex += Vector<double>.Count)
        {
            var upstreamValues = new Vector<double>(upstream.Slice(elementIndex));
            var primal = new Vector<double>(operand.Span.Slice(elementIndex));
            var adjoints = new Vector<double>(operandAdjoints.Slice(elementIndex));
            (adjoints + (upstreamValues * three * primal * primal)).CopyTo(operandAdjoints.Slice(elementIndex));
        }

        for (; elementIndex < upstream.Length; elementIndex++)
        {
            var primal = operand.Span[elementIndex];
            operandAdjoints[elementIndex] += upstream[elementIndex] * 3.0 * primal * primal;
        }
    }
}

public readonly struct CubeRootDefinition : IUnaryOperationDefinition
{
    public static Operation Operation => Operation.CubeRoot;
    public static string Name => "cbrt";
    public static bool IsDifferentiable => true;
    public static int ScratchSpanCount => 0;
    public static int AdjointScratchSpanCount => 0;
    public static OperationNotation Notation => OperationNotation.Function;

    public static double Apply(double value) => Math.Cbrt(value);
    public static void Apply(ReadOnlySpan<double> value, Span<double> result, ScratchSpans scratch) => TensorPrimitives.Cbrt(value, result);

    /// <remarks>
    /// The derivative of <c>cbrt(x)</c> is the reciprocal of three times <c>cbrt(x)</c> squared, so the retained
    /// result is used rather than recomputing a root. At <c>x = 0</c> this divides by zero and yields an infinite
    /// derivative, which is the true one.
    /// </remarks>
    public static void Adjoint(ReadOnlySpan<double> upstream, Operand operand, Operand result, Span<double> operandAdjoints, ScratchSpans scratch)
    {
        if (result.IsScalar)
        {
            TensorPrimitives.MultiplyAdd(upstream, 1.0 / (3.0 * result.Scalar * result.Scalar), operandAdjoints, operandAdjoints);
            return;
        }

        var elementIndex = 0;
        var vectorizedElementCount = TensorPrimitivesEx.VectorizedCount(upstream.Length);
        var three = new Vector<double>(3.0);
        for (; elementIndex < vectorizedElementCount; elementIndex += Vector<double>.Count)
        {
            var upstreamValues = new Vector<double>(upstream.Slice(elementIndex));
            var primal = new Vector<double>(result.Span.Slice(elementIndex));
            var adjoints = new Vector<double>(operandAdjoints.Slice(elementIndex));
            (adjoints + (upstreamValues / (three * primal * primal))).CopyTo(operandAdjoints.Slice(elementIndex));
        }

        for (; elementIndex < upstream.Length; elementIndex++)
        {
            var primal = result.Span[elementIndex];
            operandAdjoints[elementIndex] += upstream[elementIndex] / (3.0 * primal * primal);
        }
    }
}

public readonly struct SinDefinition : IUnaryOperationDefinition
{
    public static Operation Operation => Operation.Sin;
    public static string Name => "sin";
    public static bool IsDifferentiable => true;
    public static int ScratchSpanCount => 0;
    public static int AdjointScratchSpanCount => 0;
    public static OperationNotation Notation => OperationNotation.Function;

    public static double Apply(double value) => Math.Sin(value);
    public static void Apply(ReadOnlySpan<double> value, Span<double> result, ScratchSpans scratch) => TensorPrimitives.Sin(value, result);

    /// <remarks>The derivative of <c>sin(x)</c> is <c>cos(x)</c>, which the result cannot supply, so the operand is used.</remarks>
    public static void Adjoint(ReadOnlySpan<double> upstream, Operand operand, Operand result, Span<double> operandAdjoints, ScratchSpans scratch)
    {
        if (operand.IsScalar)
        {
            TensorPrimitives.MultiplyAdd(upstream, Math.Cos(operand.Scalar), operandAdjoints, operandAdjoints);
            return;
        }

        var elementIndex = 0;
        var vectorizedElementCount = TensorPrimitivesEx.VectorizedCount(upstream.Length);
        for (; elementIndex < vectorizedElementCount; elementIndex += Vector<double>.Count)
        {
            var upstreamValues = new Vector<double>(upstream.Slice(elementIndex));
            var primal = new Vector<double>(operand.Span.Slice(elementIndex));
            var adjoints = new Vector<double>(operandAdjoints.Slice(elementIndex));
            (adjoints + (upstreamValues * Vector.Cos(primal))).CopyTo(operandAdjoints.Slice(elementIndex));
        }

        for (; elementIndex < upstream.Length; elementIndex++)
        {
            operandAdjoints[elementIndex] += upstream[elementIndex] * Math.Cos(operand.Span[elementIndex]);
        }
    }
}

public readonly struct CosDefinition : IUnaryOperationDefinition
{
    public static Operation Operation => Operation.Cos;
    public static string Name => "cos";
    public static bool IsDifferentiable => true;
    public static int ScratchSpanCount => 0;
    public static int AdjointScratchSpanCount => 0;
    public static OperationNotation Notation => OperationNotation.Function;

    public static double Apply(double value) => Math.Cos(value);
    public static void Apply(ReadOnlySpan<double> value, Span<double> result, ScratchSpans scratch) => TensorPrimitives.Cos(value, result);

    /// <remarks>The derivative of <c>cos(x)</c> is <c>-sin(x)</c>.</remarks>
    public static void Adjoint(ReadOnlySpan<double> upstream, Operand operand, Operand result, Span<double> operandAdjoints, ScratchSpans scratch)
    {
        if (operand.IsScalar)
        {
            TensorPrimitives.MultiplyAdd(upstream, -Math.Sin(operand.Scalar), operandAdjoints, operandAdjoints);
            return;
        }

        var elementIndex = 0;
        var vectorizedElementCount = TensorPrimitivesEx.VectorizedCount(upstream.Length);
        for (; elementIndex < vectorizedElementCount; elementIndex += Vector<double>.Count)
        {
            var upstreamValues = new Vector<double>(upstream.Slice(elementIndex));
            var primal = new Vector<double>(operand.Span.Slice(elementIndex));
            var adjoints = new Vector<double>(operandAdjoints.Slice(elementIndex));
            (adjoints - (upstreamValues * Vector.Sin(primal))).CopyTo(operandAdjoints.Slice(elementIndex));
        }

        for (; elementIndex < upstream.Length; elementIndex++)
        {
            operandAdjoints[elementIndex] -= upstream[elementIndex] * Math.Sin(operand.Span[elementIndex]);
        }
    }
}

public readonly struct TanDefinition : IUnaryOperationDefinition
{
    public static Operation Operation => Operation.Tan;
    public static string Name => "tan";
    public static bool IsDifferentiable => true;
    public static int ScratchSpanCount => 0;
    public static int AdjointScratchSpanCount => 0;
    public static OperationNotation Notation => OperationNotation.Function;

    public static double Apply(double value) => Math.Tan(value);
    public static void Apply(ReadOnlySpan<double> value, Span<double> result, ScratchSpans scratch) => TensorPrimitives.Tan(value, result);

    /// <remarks>The derivative of <c>tan(x)</c> is one plus <c>tan(x)</c> squared, so the retained result is used.</remarks>
    public static void Adjoint(ReadOnlySpan<double> upstream, Operand operand, Operand result, Span<double> operandAdjoints, ScratchSpans scratch)
    {
        if (result.IsScalar)
        {
            TensorPrimitives.MultiplyAdd(upstream, 1.0 + (result.Scalar * result.Scalar), operandAdjoints, operandAdjoints);
            return;
        }

        var elementIndex = 0;
        var vectorizedElementCount = TensorPrimitivesEx.VectorizedCount(upstream.Length);
        for (; elementIndex < vectorizedElementCount; elementIndex += Vector<double>.Count)
        {
            var upstreamValues = new Vector<double>(upstream.Slice(elementIndex));
            var primal = new Vector<double>(result.Span.Slice(elementIndex));
            var adjoints = new Vector<double>(operandAdjoints.Slice(elementIndex));
            (adjoints + (upstreamValues * (Vector<double>.One + (primal * primal)))).CopyTo(operandAdjoints.Slice(elementIndex));
        }

        for (; elementIndex < upstream.Length; elementIndex++)
        {
            operandAdjoints[elementIndex] += upstream[elementIndex] * (1.0 + (result.Span[elementIndex] * result.Span[elementIndex]));
        }
    }
}

public readonly struct TanhDefinition : IUnaryOperationDefinition
{
    public static Operation Operation => Operation.Tanh;
    public static string Name => "tanh";
    public static bool IsDifferentiable => true;
    public static int ScratchSpanCount => 0;
    public static int AdjointScratchSpanCount => 0;
    public static OperationNotation Notation => OperationNotation.Function;

    public static double Apply(double value) => Math.Tanh(value);
    public static void Apply(ReadOnlySpan<double> value, Span<double> result, ScratchSpans scratch) => TensorPrimitives.Tanh(value, result);

    /// <remarks>The derivative of <c>tanh(x)</c> is one minus <c>tanh(x)</c> squared, so the retained result is used.</remarks>
    public static void Adjoint(ReadOnlySpan<double> upstream, Operand operand, Operand result, Span<double> operandAdjoints, ScratchSpans scratch)
    {
        if (result.IsScalar)
        {
            TensorPrimitives.MultiplyAdd(upstream, 1.0 - (result.Scalar * result.Scalar), operandAdjoints, operandAdjoints);
            return;
        }

        var elementIndex = 0;
        var vectorizedElementCount = TensorPrimitivesEx.VectorizedCount(upstream.Length);
        for (; elementIndex < vectorizedElementCount; elementIndex += Vector<double>.Count)
        {
            var upstreamValues = new Vector<double>(upstream.Slice(elementIndex));
            var primal = new Vector<double>(result.Span.Slice(elementIndex));
            var adjoints = new Vector<double>(operandAdjoints.Slice(elementIndex));
            (adjoints + (upstreamValues * (Vector<double>.One - (primal * primal)))).CopyTo(operandAdjoints.Slice(elementIndex));
        }

        for (; elementIndex < upstream.Length; elementIndex++)
        {
            operandAdjoints[elementIndex] += upstream[elementIndex] * (1.0 - (result.Span[elementIndex] * result.Span[elementIndex]));
        }
    }
}

public readonly struct AddDefinition : IBinaryOperationDefinition
{
    public static Operation Operation => Operation.Add;
    public static string Name => "+";
    public static bool IsDifferentiable => true;
    public static int ScratchSpanCount => 0;
    public static int AdjointScratchSpanCount => 0;
    public static OperationNotation Notation => OperationNotation.Infix;

    public static double Apply(double left, double right) => left + right;
    public static void Apply(ReadOnlySpan<double> left, double right, Span<double> result, ScratchSpans scratch) => TensorPrimitives.Add(left, right, result);
    public static void Apply(double left, ReadOnlySpan<double> right, Span<double> result, ScratchSpans scratch) => TensorPrimitives.Add(right, left, result);
    public static void Apply(ReadOnlySpan<double> left, ReadOnlySpan<double> right, Span<double> result, ScratchSpans scratch) => TensorPrimitives.Add(left, right, result);

    /// <remarks>Both partials are one, so each active operand receives the upstream adjoints unchanged.</remarks>
    public static void Adjoint(ReadOnlySpan<double> upstream, Operand left, Operand right, Operand result, Span<double> leftAdjoints, bool leftIsActive, Span<double> rightAdjoints, bool rightIsActive, ScratchSpans scratch)
    {
        if (leftIsActive)
            TensorPrimitives.MultiplyAdd(upstream, 1.0, leftAdjoints, leftAdjoints);

        if (rightIsActive)
            TensorPrimitives.MultiplyAdd(upstream, 1.0, rightAdjoints, rightAdjoints);
    }
}

public readonly struct SubtractDefinition : IBinaryOperationDefinition
{
    public static Operation Operation => Operation.Subtract;
    public static string Name => "-";
    public static bool IsDifferentiable => true;
    public static int ScratchSpanCount => 0;
    public static int AdjointScratchSpanCount => 0;
    public static OperationNotation Notation => OperationNotation.Infix;

    public static double Apply(double left, double right) => left - right;
    public static void Apply(ReadOnlySpan<double> left, double right, Span<double> result, ScratchSpans scratch) => TensorPrimitives.Subtract(left, right, result);
    public static void Apply(double left, ReadOnlySpan<double> right, Span<double> result, ScratchSpans scratch) => TensorPrimitives.Subtract(left, right, result);
    public static void Apply(ReadOnlySpan<double> left, ReadOnlySpan<double> right, Span<double> result, ScratchSpans scratch) => TensorPrimitives.Subtract(left, right, result);

    /// <remarks>The partials are one and minus one.</remarks>
    public static void Adjoint(ReadOnlySpan<double> upstream, Operand left, Operand right, Operand result, Span<double> leftAdjoints, bool leftIsActive, Span<double> rightAdjoints, bool rightIsActive, ScratchSpans scratch)
    {
        if (leftIsActive)
            TensorPrimitives.MultiplyAdd(upstream, 1.0, leftAdjoints, leftAdjoints);

        if (rightIsActive)
            TensorPrimitives.MultiplyAdd(upstream, -1.0, rightAdjoints, rightAdjoints);
    }
}

public readonly struct MultiplyDefinition : IBinaryOperationDefinition
{
    public static Operation Operation => Operation.Multiply;
    public static string Name => "*";
    public static bool IsDifferentiable => true;
    public static int ScratchSpanCount => 0;
    public static int AdjointScratchSpanCount => 0;
    public static OperationNotation Notation => OperationNotation.Infix;

    public static double Apply(double left, double right) => left * right;
    public static void Apply(ReadOnlySpan<double> left, double right, Span<double> result, ScratchSpans scratch) => TensorPrimitives.Multiply(left, right, result);
    public static void Apply(double left, ReadOnlySpan<double> right, Span<double> result, ScratchSpans scratch) => TensorPrimitives.Multiply(right, left, result);
    public static void Apply(ReadOnlySpan<double> left, ReadOnlySpan<double> right, Span<double> result, ScratchSpans scratch) => TensorPrimitives.Multiply(left, right, result);

    /// <remarks>Each operand's partial is the other operand.</remarks>
    public static void Adjoint(ReadOnlySpan<double> upstream, Operand left, Operand right, Operand result, Span<double> leftAdjoints, bool leftIsActive, Span<double> rightAdjoints, bool rightIsActive, ScratchSpans scratch)
    {
        if (leftIsActive)
            TensorPrimitivesEx.MultiplyAdd(upstream, right, leftAdjoints, leftAdjoints);

        if (rightIsActive)
            TensorPrimitivesEx.MultiplyAdd(upstream, left, rightAdjoints, rightAdjoints);
    }
}

public readonly struct DivideDefinition : IBinaryOperationDefinition
{
    public static Operation Operation => Operation.Divide;
    public static string Name => "/";
    public static bool IsDifferentiable => true;
    public static int ScratchSpanCount => 0;
    public static int AdjointScratchSpanCount => 0;
    public static OperationNotation Notation => OperationNotation.Infix;

    public static double Apply(double left, double right) => left / right;
    public static void Apply(ReadOnlySpan<double> left, double right, Span<double> result, ScratchSpans scratch) => TensorPrimitives.Divide(left, right, result);
    public static void Apply(double left, ReadOnlySpan<double> right, Span<double> result, ScratchSpans scratch) => TensorPrimitives.Divide(left, right, result);
    public static void Apply(ReadOnlySpan<double> left, ReadOnlySpan<double> right, Span<double> result, ScratchSpans scratch) => TensorPrimitives.Divide(left, right, result);

    /// <remarks>
    /// For <c>a / b</c> the partials are <c>1 / b</c> and <c>-a / b</c> squared. The numerator partial is a shared
    /// shape; the denominator partial is specific enough to this operation to stay with it.
    /// </remarks>
    public static void Adjoint(ReadOnlySpan<double> upstream, Operand left, Operand right, Operand result, Span<double> leftAdjoints, bool leftIsActive, Span<double> rightAdjoints, bool rightIsActive, ScratchSpans scratch)
    {
        if (leftIsActive)
            TensorPrimitivesEx.DivideAdd(upstream, right, leftAdjoints, leftAdjoints);

        if (rightIsActive)
            AccumulateDenominator(upstream, left, right, rightAdjoints);
    }

    private static void AccumulateDenominator(ReadOnlySpan<double> upstream, Operand numerator, Operand denominator, Span<double> denominatorAdjoints)
    {
        if (numerator.IsScalar && denominator.IsScalar)
        {
            var factor = -numerator.Scalar / (denominator.Scalar * denominator.Scalar);
            TensorPrimitives.MultiplyAdd(upstream, factor, denominatorAdjoints, denominatorAdjoints);
            return;
        }

        var elementIndex = 0;
        var vectorizedElementCount = TensorPrimitivesEx.VectorizedCount(upstream.Length);
        if (numerator.IsScalar)
        {
            var numeratorValue = new Vector<double>(numerator.Scalar);
            for (; elementIndex < vectorizedElementCount; elementIndex += Vector<double>.Count)
            {
                var upstreamValues = new Vector<double>(upstream.Slice(elementIndex));
                var denominatorValue = new Vector<double>(denominator.Span.Slice(elementIndex));
                var adjoints = new Vector<double>(denominatorAdjoints.Slice(elementIndex));
                (adjoints - ((upstreamValues * numeratorValue) / (denominatorValue * denominatorValue))).CopyTo(denominatorAdjoints.Slice(elementIndex));
            }

            for (; elementIndex < upstream.Length; elementIndex++)
            {
                denominatorAdjoints[elementIndex] -= upstream[elementIndex] * numerator.Scalar / (denominator.Span[elementIndex] * denominator.Span[elementIndex]);
            }

            return;
        }

        if (denominator.IsScalar)
        {
            var inverseSquared = -1.0 / (denominator.Scalar * denominator.Scalar);
            var scale = new Vector<double>(inverseSquared);
            for (; elementIndex < vectorizedElementCount; elementIndex += Vector<double>.Count)
            {
                var upstreamValues = new Vector<double>(upstream.Slice(elementIndex));
                var numeratorValue = new Vector<double>(numerator.Span.Slice(elementIndex));
                var adjoints = new Vector<double>(denominatorAdjoints.Slice(elementIndex));
                (adjoints + (upstreamValues * numeratorValue * scale)).CopyTo(denominatorAdjoints.Slice(elementIndex));
            }

            for (; elementIndex < upstream.Length; elementIndex++)
            {
                denominatorAdjoints[elementIndex] += upstream[elementIndex] * numerator.Span[elementIndex] * inverseSquared;
            }

            return;
        }

        for (; elementIndex < vectorizedElementCount; elementIndex += Vector<double>.Count)
        {
            var upstreamValues = new Vector<double>(upstream.Slice(elementIndex));
            var numeratorValue = new Vector<double>(numerator.Span.Slice(elementIndex));
            var denominatorValue = new Vector<double>(denominator.Span.Slice(elementIndex));
            var adjoints = new Vector<double>(denominatorAdjoints.Slice(elementIndex));
            (adjoints - ((upstreamValues * numeratorValue) / (denominatorValue * denominatorValue))).CopyTo(denominatorAdjoints.Slice(elementIndex));
        }

        for (; elementIndex < upstream.Length; elementIndex++)
        {
            denominatorAdjoints[elementIndex] -= upstream[elementIndex] * numerator.Span[elementIndex] / (denominator.Span[elementIndex] * denominator.Span[elementIndex]);
        }
    }
}

public readonly struct PowerDefinition : IBinaryOperationDefinition
{
    public static Operation Operation => Operation.Power;
    public static string Name => "pow";
    public static bool IsDifferentiable => true;
    public static int ScratchSpanCount => 0;
    public static int AdjointScratchSpanCount => 2;
    public static OperationNotation Notation => OperationNotation.Function;

    public static double Apply(double left, double right) => Math.Pow(left, right);
    public static void Apply(ReadOnlySpan<double> left, double right, Span<double> result, ScratchSpans scratch) => TensorPrimitives.Pow(left, right, result);
    public static void Apply(double left, ReadOnlySpan<double> right, Span<double> result, ScratchSpans scratch) => TensorPrimitives.Pow(left, right, result);
    public static void Apply(ReadOnlySpan<double> left, ReadOnlySpan<double> right, Span<double> result, ScratchSpans scratch) => TensorPrimitives.Pow(left, right, result);

    /// <remarks>
    /// For <c>a ^ b</c> the partials are <c>b * a^(b-1)</c> and <c>a^b * ln(a)</c>. The exponent partial is only
    /// defined for a positive base; a nonpositive one yields NaN through ordinary IEEE propagation, as elsewhere in
    /// the engine.
    /// </remarks>
    public static void Adjoint(ReadOnlySpan<double> upstream, Operand left, Operand right, Operand result, Span<double> leftAdjoints, bool leftIsActive, Span<double> rightAdjoints, bool rightIsActive, ScratchSpans scratch)
    {
        if (leftIsActive)
        {
            TensorPrimitivesEx.Subtract(right, 1.0, scratch[0]);
            TensorPrimitivesEx.Pow(left, scratch[0], scratch[1]);
            TensorPrimitivesEx.Multiply(scratch[1], right, scratch[1]);
            TensorPrimitives.MultiplyAdd(upstream, scratch[1], leftAdjoints, leftAdjoints);
        }

        if (rightIsActive)
        {
            TensorPrimitivesEx.Log(left, scratch[0]);
            TensorPrimitivesEx.Multiply(upstream, result, scratch[1]);
            TensorPrimitives.MultiplyAdd(scratch[1], scratch[0], rightAdjoints, rightAdjoints);
        }
    }
}

/// <remarks>The reciprocal of a span of degrees has to be built before it can be used as an exponent.</remarks>
public readonly struct RootDefinition : IBinaryOperationDefinition
{
    public static Operation Operation => Operation.Root;
    public static string Name => "root";
    public static bool IsDifferentiable => true;
    public static int ScratchSpanCount => 1;
    public static int AdjointScratchSpanCount => 2;
    public static OperationNotation Notation => OperationNotation.Function;

    public static double Apply(double left, double right) => Math.Pow(left, 1.0 / right);

    public static void Apply(ReadOnlySpan<double> left, double right, Span<double> result, ScratchSpans scratch) =>
        TensorPrimitives.Pow(left, 1.0 / right, result);

    public static void Apply(double left, ReadOnlySpan<double> right, Span<double> result, ScratchSpans scratch)
    {
        TensorPrimitives.Reciprocal(right, scratch[0]);
        TensorPrimitives.Pow(left, scratch[0], result);
    }

    public static void Apply(ReadOnlySpan<double> left, ReadOnlySpan<double> right, Span<double> result, ScratchSpans scratch)
    {
        TensorPrimitives.Reciprocal(right, scratch[0]);
        TensorPrimitives.Pow(left, scratch[0], result);
    }

    /// <remarks>
    /// For <c>a ^ (1/b)</c> the partials are <c>(1/b) * a^(1/b - 1)</c> and the negated <c>a^(1/b) * ln(a) / b</c>
    /// squared. As with <see cref="PowerDefinition"/>, the second partial requires a positive base.
    /// </remarks>
    public static void Adjoint(ReadOnlySpan<double> upstream, Operand left, Operand right, Operand result, Span<double> leftAdjoints, bool leftIsActive, Span<double> rightAdjoints, bool rightIsActive, ScratchSpans scratch)
    {
        if (leftIsActive)
        {
            TensorPrimitivesEx.Reciprocal(right, scratch[0]);
            TensorPrimitives.Subtract(scratch[0], 1.0, scratch[1]);
            TensorPrimitivesEx.Pow(left, scratch[1], scratch[1]);
            TensorPrimitives.Multiply(scratch[1], scratch[0], scratch[1]);
            TensorPrimitives.MultiplyAdd(upstream, scratch[1], leftAdjoints, leftAdjoints);
        }

        if (rightIsActive)
        {
            TensorPrimitivesEx.Log(left, scratch[0]);
            TensorPrimitivesEx.Multiply(upstream, result, scratch[1]);
            TensorPrimitives.Multiply(scratch[1], scratch[0], scratch[1]);
            TensorPrimitivesEx.Square(right, scratch[0]);
            TensorPrimitives.Divide(scratch[1], scratch[0], scratch[1]);
            TensorPrimitives.Subtract(rightAdjoints, scratch[1], rightAdjoints);
        }
    }
}

/// <remarks>The denominator span has to be built before the division can run.</remarks>
public readonly struct AnalyticQuotientDefinition : IBinaryOperationDefinition
{
    public static Operation Operation => Operation.AnalyticQuotient;
    public static string Name => "aq";
    public static bool IsDifferentiable => true;
    public static int ScratchSpanCount => 1;
    public static int AdjointScratchSpanCount => 2;
    public static OperationNotation Notation => OperationNotation.Function;

    public static double Apply(double left, double right) => left / Math.Sqrt(1.0 + (right * right));

    public static void Apply(ReadOnlySpan<double> left, double right, Span<double> result, ScratchSpans scratch) =>
        TensorPrimitives.Divide(left, Math.Sqrt(1.0 + (right * right)), result);

    public static void Apply(double left, ReadOnlySpan<double> right, Span<double> result, ScratchSpans scratch)
    {
        BuildDenominator(right, scratch[0]);
        TensorPrimitives.Divide(left, scratch[0], result);
    }

    public static void Apply(ReadOnlySpan<double> left, ReadOnlySpan<double> right, Span<double> result, ScratchSpans scratch)
    {
        BuildDenominator(right, scratch[0]);
        TensorPrimitives.Divide(left, scratch[0], result);
    }

    /// <remarks>
    /// The analytic quotient <c>a / sqrt(1 + b²)</c> is the division substitute that avoids a pole, so both partials
    /// are defined everywhere: <c>1 / sqrt(1 + b²)</c> and <c>-a * b / (1 + b²)^(3/2)</c>.
    /// </remarks>
    public static void Adjoint(ReadOnlySpan<double> upstream, Operand left, Operand right, Operand result, Span<double> leftAdjoints, bool leftIsActive, Span<double> rightAdjoints, bool rightIsActive, ScratchSpans scratch)
    {
        // Both partials are written in terms of 1 + b squared and its root, so both are built once up front.
        TensorPrimitivesEx.Square(right, scratch[0]);
        TensorPrimitives.Add(scratch[0], 1.0, scratch[0]);
        TensorPrimitives.Sqrt(scratch[0], scratch[1]);

        if (leftIsActive)
            TensorPrimitivesEx.DivideAdd(upstream, Operand.FromSpan(scratch[1]), leftAdjoints, leftAdjoints);

        if (rightIsActive)
        {
            TensorPrimitives.Multiply(scratch[1], scratch[0], scratch[0]);
            TensorPrimitivesEx.Multiply(upstream, left, scratch[1]);
            TensorPrimitivesEx.Multiply(scratch[1], right, scratch[1]);
            TensorPrimitives.Divide(scratch[1], scratch[0], scratch[1]);
            TensorPrimitives.Subtract(rightAdjoints, scratch[1], rightAdjoints);
        }
    }

    private static void BuildDenominator(ReadOnlySpan<double> right, Span<double> scratch)
    {
        TensorPrimitives.Multiply(right, right, scratch);
        TensorPrimitives.Add(scratch, 1.0, scratch);
        TensorPrimitives.Sqrt(scratch, scratch);
    }
}
