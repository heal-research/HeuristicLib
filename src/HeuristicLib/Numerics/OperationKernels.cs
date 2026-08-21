namespace HEAL.HeuristicLib.Numerics;

internal delegate double UnaryScalarKernel(double value);

internal delegate void UnarySpanKernel(ReadOnlySpan<double> value, Span<double> result, ScratchSpans scratch);

internal delegate double BinaryScalarKernel(double left, double right);

internal delegate void BinarySpanScalarKernel(ReadOnlySpan<double> left, double right, Span<double> result, ScratchSpans scratch);

internal delegate void BinaryScalarSpanKernel(double left, ReadOnlySpan<double> right, Span<double> result, ScratchSpans scratch);

internal delegate void BinarySpanKernel(ReadOnlySpan<double> left, ReadOnlySpan<double> right, Span<double> result, ScratchSpans scratch);

internal delegate void UnaryAdjointKernel(ReadOnlySpan<double> upstream, Operand operand, Operand result, Span<double> operandAdjoints, ScratchSpans scratch);

internal delegate void BinaryAdjointKernel(ReadOnlySpan<double> upstream, Operand left, Operand right, Operand result, Span<double> leftAdjoints, bool leftIsActive, Span<double> rightAdjoints, bool rightIsActive, ScratchSpans scratch);

/// <remarks>
/// Both shapes always exist, because a unary operation is required to declare both. The delegates are handles to the
/// operation's static methods, which is what lets a table hold them.
/// </remarks>
internal readonly record struct UnaryOperationKernels(
    UnaryScalarKernel Scalar,
    UnarySpanKernel Span,
    UnaryAdjointKernel Adjoint);

/// <remarks>All four shapes always exist, because a binary operation is required to declare all four.</remarks>
internal readonly record struct BinaryOperationKernels(
    BinaryScalarKernel Scalar,
    BinarySpanScalarKernel SpanScalar,
    BinaryScalarSpanKernel ScalarSpan,
    BinarySpanKernel Span,
    BinaryAdjointKernel Adjoint);
